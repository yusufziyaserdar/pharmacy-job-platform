using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.ViewModels;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{

    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 📥 Inbox
        public IActionResult Index()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var conversations = _context.Conversations
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

            var msg = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            return RedirectToAction("Chat", new { id = conversationId });
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
    }




}
