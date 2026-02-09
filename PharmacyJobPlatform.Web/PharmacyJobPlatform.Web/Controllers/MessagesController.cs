using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.ViewModels;
using PharmacyJobPlatform.Web.SignalR;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{

    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessagesController(ApplicationDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // 📥 Inbox
        public IActionResult Index()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var conversations = GetInboxConversations(userId);

            var requests = _context.ConversationRequests
                .Include(r => r.FromUser)
                .Where(r => r.ToUserId == userId && !r.IsAccepted)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ConversationRequestViewModel
                {
                    Id = r.Id,
                    FromUserId = r.FromUserId,
                    FromUserName = r.FromUser.FirstName + " " + r.FromUser.LastName,
                    FromUserProfileImagePath = r.FromUser.ProfileImagePath,
                    CreatedAt = r.CreatedAt
                })
                .ToList();

            var vm = new MessagesInboxViewModel
            {
                Conversations = conversations,
                IncomingRequests = requests
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult InboxConversations()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var conversations = GetInboxConversations(userId);
            return PartialView("_InboxConversations", conversations);
        }

        // 💬 Chat ekranı
        public async Task<IActionResult> Chat(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var conv = await _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    (c.User1Id == userId || c.User2Id == userId));

            if (conv == null)
                return Unauthorized();

            foreach (var msg in conv.Messages
                .Where(m => m.SenderId != userId && !m.IsRead))
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();

            return View(conv);
        }

        // ✉ Mesaj gönder
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SendMessage(int conversationId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("Chat", new { id = conversationId });

            int senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));


            bool sent = await TrySendMessageAsync(conversationId, senderId, content);
            if (!sent)
                return RedirectToAction("Chat", new { id = conversationId });


            return RedirectToAction("Chat", new { id = conversationId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SendMessageAjax(int conversationId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest();

            int senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            bool sent = await TrySendMessageAsync(conversationId, senderId, content);

            if (!sent)
                return NotFound();

            return Ok();
        }


        // 📩 Mesaj isteği gönder
        [HttpPost]
        public async Task<IActionResult> Start(int userId)
        {
            int senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (senderId == userId)
                return RedirectToAction("Index", "Profile", new { id = userId });

            var existingConversation = _context.Conversations
                .FirstOrDefault(c =>
                    (c.User1Id == senderId && c.User2Id == userId) ||
                    (c.User1Id == userId && c.User2Id == senderId));

            if (existingConversation != null)
                return RedirectToAction("Chat", new { id = existingConversation.Id });

            bool hasPendingRequest = _context.ConversationRequests.Any(r =>
                !r.IsAccepted &&
                ((r.FromUserId == senderId && r.ToUserId == userId) ||
                 (r.FromUserId == userId && r.ToUserId == senderId)));

            if (!hasPendingRequest)
            {
                _context.ConversationRequests.Add(new ConversationRequest
                {
                    FromUserId = senderId,
                    ToUserId = userId
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Profile", new { id = userId });
        }

        // ✅ Mesaj isteği kabul et
        [HttpPost]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var request = await _context.ConversationRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ToUserId == userId);

            if (request == null)
                return NotFound();

            if (!request.IsAccepted)
            {
                request.IsAccepted = true;
            }

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c =>
                    (c.User1Id == request.FromUserId && c.User2Id == request.ToUserId) ||
                    (c.User1Id == request.ToUserId && c.User2Id == request.FromUserId));

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    User1Id = request.FromUserId,
                    User2Id = request.ToUserId
                };
                _context.Conversations.Add(conversation);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Chat", new { id = conversation.Id });
        }

        // 🧩 Widget – conversation list
        [HttpGet]
        public IActionResult WidgetConversations()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var data = _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .Select(c => new ChatWidgetConversationVM
                {
                    ConversationId = c.Id,

                    OtherUserId = c.User1Id == userId
                        ? c.User2Id
                        : c.User1Id,

                    OtherUserName = c.User1Id == userId
                        ? c.User2.FirstName + " " + c.User2.LastName
                        : c.User1.FirstName + " " + c.User1.LastName,

                    LastMessage = c.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.Content)
                        .FirstOrDefault(),

                    UnreadCount = c.Messages.Count(m =>
                        !m.IsRead && m.SenderId != userId)
                })
                .ToList();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> InboxMessages(int conversationId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var conv = await _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c =>
                    c.Id == conversationId &&
                    (c.User1Id == userId || c.User2Id == userId));

            if (conv == null)
                return Unauthorized();

            foreach (var msg in conv.Messages
                .Where(m => !m.IsRead && m.SenderId != userId))
            {
                msg.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return PartialView("_InboxMessages", conv);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConversation(int conversationId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var conversation = await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c =>
                    c.Id == conversationId &&
                    (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null)
                return Unauthorized();

            _context.Messages.RemoveRange(conversation.Messages);
            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var message = await _context.Messages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
                return NotFound();

            if (message.Conversation.User1Id != userId && message.Conversation.User2Id != userId)
                return Unauthorized();

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return Ok();
        }


        // 🧩 Widget – messages
        [HttpGet]
        public async Task<IActionResult> WidgetChat(int conversationId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var conv = await _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c =>
                    c.Id == conversationId &&
                    (c.User1Id == userId || c.User2Id == userId));


            if (conv == null)
                return Unauthorized();

            foreach (var msg in conv.Messages
                .Where(m => !m.IsRead && m.SenderId != userId))
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();

            return PartialView("_ChatMessages", conv);
        }

        [HttpGet]
        public async Task<IActionResult> ChatMessages(int conversationId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var conv = await _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c =>
                    c.Id == conversationId &&
                    (c.User1Id == userId || c.User2Id == userId));

            if (conv == null)
                return Unauthorized();

            foreach (var msg in conv.Messages
                .Where(m => !m.IsRead && m.SenderId != userId))
            {
                msg.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return PartialView("_ChatBodyMessages", conv);
        }

        [HttpGet]
        public IActionResult UnreadCount()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            int unreadCount = _context.Messages.Count(m =>
                !m.IsRead &&
                m.SenderId != userId &&
                (m.Conversation.User1Id == userId ||
                 m.Conversation.User2Id == userId));

            return Json(new { unreadCount });
        }

        private async Task<bool> TrySendMessageAsync(int conversationId, int senderId, string content)
        {
            var conversation = await _context.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return false;

            var msg = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            var receiverId = conversation.User1Id == senderId
                ? conversation.User2Id
                : conversation.User1Id;

            await _hubContext.Clients.Users(senderId.ToString(), receiverId.ToString())
                .SendAsync("RefreshMessages", conversationId);

            return true;
        }

        private List<InboxConversationViewModel> GetInboxConversations(int userId)
        {
            return _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .Select(c => new InboxConversationViewModel
                {
                    ConversationId = c.Id,

                    OtherUserId = c.User1Id == userId ? c.User2Id : c.User1Id,

                    OtherUserFullName = c.User1Id == userId
                        ? c.User2.FirstName + " " + c.User2.LastName
                        : c.User1.FirstName + " " + c.User1.LastName,

                    LastMessage = c.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.Content)
                        .FirstOrDefault(),

                    LastMessageTime = c.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.SentAt)
                        .FirstOrDefault(),

                    UnreadCount = c.Messages.Count(m =>
                        !m.IsRead && m.SenderId != userId)
                })
                .OrderByDescending(x => x.LastMessageTime)
                .ToList();
        }
    }




}
