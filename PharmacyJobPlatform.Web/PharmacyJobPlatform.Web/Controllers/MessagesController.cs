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
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(
            ApplicationDbContext context,
            IHubContext<ChatHub> hubContext,
            ILogger<MessagesController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

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

        [HttpGet]
        public IActionResult SearchUsers(string term)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            string searchTerm = term?.Trim() ?? string.Empty;

            if (searchTerm.Length < 2)
                return Json(Array.Empty<object>());

            var interactedUserIdsQuery = _context.Conversations
                .Where(c => !c.EndedAt.HasValue && (c.User1Id == userId || c.User2Id == userId))
                .Select(c => c.User1Id == userId ? c.User2Id : c.User1Id)
                .Concat(_context.ConversationRequests
                    .Where(r => r.FromUserId == userId || r.ToUserId == userId)
                    .Select(r => r.FromUserId == userId ? r.ToUserId : r.FromUserId))
                .Distinct();

            var users = _context.Users
                .Where(u => u.Id != userId)
                .Where(u => EF.Functions.Like((u.FirstName + " " + u.LastName), $"%{searchTerm}%"))
                .Select(u => new
                {
                    id = u.Id,
                    fullName = u.FirstName + " " + u.LastName,
                    isInteracted = interactedUserIdsQuery.Contains(u.Id)
                })
                .OrderByDescending(u => u.isInteracted)
                .ThenBy(u => u.fullName)
                .Take(8)
                .ToList();

            return Json(users);
        }

        public async Task<IActionResult> Chat(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var conv = await _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    !c.EndedAt.HasValue &&
                    (c.User1Id == userId || c.User2Id == userId) &&
                    ((c.User1Id == userId && !c.User1Deleted) || (c.User2Id == userId && !c.User2Deleted)));

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
            if (conversationId <= 0)
                return BadRequest();

            if (string.IsNullOrWhiteSpace(content))
                return BadRequest();

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int senderId))
                return Unauthorized();

            bool sent;
            try
            {
                sent = await TrySendMessageAsync(conversationId, senderId, content.Trim());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SendMessageAjax hatası. ConversationId: {ConversationId}, SenderId: {SenderId}",
                    conversationId,
                    senderId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            if (!sent)
                return NotFound();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Start(int userId)
        {
            int senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (senderId == userId)
                return RedirectToAction("Index", "Profile", new { id = userId });

            var existingConversation = _context.Conversations
                .FirstOrDefault(c =>
                    !c.EndedAt.HasValue &&
                    ((c.User1Id == senderId && c.User2Id == userId) ||
                     (c.User1Id == userId && c.User2Id == senderId)));

            if (existingConversation != null)
            {
                if (existingConversation.User1Id == senderId)
                    existingConversation.User1Deleted = false;
                else
                    existingConversation.User2Deleted = false;

                await _context.SaveChangesAsync();
                return RedirectToAction("Chat", new { id = existingConversation.Id });
            }

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
                    !c.EndedAt.HasValue &&
                    ((c.User1Id == request.FromUserId && c.User2Id == request.ToUserId) ||
                     (c.User1Id == request.ToUserId && c.User2Id == request.FromUserId)));

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    User1Id = request.FromUserId,
                    User2Id = request.ToUserId
                };
                _context.Conversations.Add(conversation);
            }

            if (conversation.User1Id == userId)
                conversation.User1Deleted = false;
            else
                conversation.User2Deleted = false;

            await _context.SaveChangesAsync();

            return RedirectToAction("Chat", new { id = conversation.Id });
        }

        [HttpGet]
        public IActionResult WidgetConversations()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var data = _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Where(c =>
                    !c.EndedAt.HasValue &&
                    (c.User1Id == userId || c.User2Id == userId) &&
                    ((c.User1Id == userId && !c.User1Deleted) || (c.User2Id == userId && !c.User2Deleted)))
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
                        .Where(m => (m.SenderId == userId ? !m.DeletedBySender : !m.DeletedByReceiver))
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.IsRecalled ? "Bu mesaj karşı taraf tarafından geri alındı." : m.Content)
                        .FirstOrDefault(),

                    UnreadCount = c.Messages.Count(m =>
                        (m.SenderId == userId ? !m.DeletedBySender : !m.DeletedByReceiver) && !m.IsRead && m.SenderId != userId)
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
                    !c.EndedAt.HasValue &&
                    (c.User1Id == userId || c.User2Id == userId) &&
                    ((c.User1Id == userId && !c.User1Deleted) || (c.User2Id == userId && !c.User2Deleted)));

            if (conv == null)
                return Unauthorized();

            foreach (var msg in conv.Messages
                .Where(m => (m.SenderId == userId ? !m.DeletedBySender : !m.DeletedByReceiver) && !m.IsRead && m.SenderId != userId))
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
                .FirstOrDefaultAsync(c =>
                    c.Id == conversationId &&
                    !c.EndedAt.HasValue &&
                    (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null)
                return Unauthorized();

            if (conversation.User1Id == userId)
                conversation.User1Deleted = true;
            else
                conversation.User2Deleted = true;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> EndConversation(int conversationId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c =>
                    c.Id == conversationId &&
                    !c.EndedAt.HasValue &&
                    (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null)
                return Unauthorized();

            conversation.EndedAt = DateTime.UtcNow;
            conversation.EndedByUserId = userId;

            await _context.SaveChangesAsync();

            var otherUserId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
            await _hubContext.Clients.Users(userId.ToString(), otherUserId.ToString())
                .SendAsync("RefreshMessages", conversation.Id);

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

            if (message.Conversation.EndedAt.HasValue)
                return BadRequest();

            if (message.SenderId == userId)
                message.DeletedBySender = true;
            else
                message.DeletedByReceiver = true;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RecallMessage(int messageId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var message = await _context.Messages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
                return NotFound();

            if (message.SenderId != userId)
                return Unauthorized();

            if (message.Conversation.EndedAt.HasValue)
                return BadRequest();

            message.IsRecalled = true;
            message.RecalledAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var otherUserId = message.Conversation.User1Id == userId
                ? message.Conversation.User2Id
                : message.Conversation.User1Id;

            await _hubContext.Clients.Users(userId.ToString(), otherUserId.ToString())
                .SendAsync("RefreshMessages", message.ConversationId);

            return Ok();
        }

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
                    !c.EndedAt.HasValue &&
                    (c.User1Id == userId || c.User2Id == userId) &&
                    ((c.User1Id == userId && !c.User1Deleted) || (c.User2Id == userId && !c.User2Deleted)));


            if (conv == null)
                return Unauthorized();

            foreach (var msg in conv.Messages
                .Where(m => (m.SenderId == userId ? !m.DeletedBySender : !m.DeletedByReceiver) && !m.IsRead && m.SenderId != userId))
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
                    !c.EndedAt.HasValue &&
                    (c.User1Id == userId || c.User2Id == userId) &&
                    ((c.User1Id == userId && !c.User1Deleted) || (c.User2Id == userId && !c.User2Deleted)));

            if (conv == null)
                return Unauthorized();

            foreach (var msg in conv.Messages
                .Where(m => (m.SenderId == userId ? !m.DeletedBySender : !m.DeletedByReceiver) && !m.IsRead && m.SenderId != userId))
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
                (m.SenderId == userId ? !m.DeletedBySender : !m.DeletedByReceiver) &&
                !m.IsRead &&
                m.SenderId != userId &&
                !m.Conversation.EndedAt.HasValue &&
                ((m.Conversation.User1Id == userId && !m.Conversation.User1Deleted) || (m.Conversation.User2Id == userId && !m.Conversation.User2Deleted)) &&
                (m.Conversation.User1Id == userId ||
                 m.Conversation.User2Id == userId));

            return Json(new { unreadCount });
        }

        private async Task<bool> TrySendMessageAsync(int conversationId, int senderId, string content)
        {
            var conversation = await _context.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c =>
                    c.Id == conversationId &&
                    !c.EndedAt.HasValue &&
                    (c.User1Id == senderId || c.User2Id == senderId) &&
                    !IsConversationDeletedForUser(c, senderId));

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

            try
            {
                await _hubContext.Clients.Users(senderId.ToString(), receiverId.ToString())
                    .SendAsync("RefreshMessages", conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Message {ConversationId} kaydedildi fakat SignalR bildirimi gönderilemedi. SenderId: {SenderId}, ReceiverId: {ReceiverId}",
                    conversationId,
                    senderId,
                    receiverId);
            }

            return true;
        }

        private List<InboxConversationViewModel> GetInboxConversations(int userId)
        {
            return _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Where(c =>
                    !c.EndedAt.HasValue &&
                    (c.User1Id == userId || c.User2Id == userId) &&
                    ((c.User1Id == userId && !c.User1Deleted) || (c.User2Id == userId && !c.User2Deleted)))
                .Select(c => new InboxConversationViewModel
                {
                    ConversationId = c.Id,

                    OtherUserId = c.User1Id == userId ? c.User2Id : c.User1Id,

                    OtherUserFullName = c.User1Id == userId
                        ? c.User2.FirstName + " " + c.User2.LastName
                        : c.User1.FirstName + " " + c.User1.LastName,

                    LastMessage = c.Messages
                        .Where(m => (m.SenderId == userId ? !m.DeletedBySender : !m.DeletedByReceiver))
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.IsRecalled ? "Bu mesaj karşı taraf tarafından geri alındı." : m.Content)
                        .FirstOrDefault(),

                    LastMessageTime = c.Messages
                        .Where(m => (m.SenderId == userId ? !m.DeletedBySender : !m.DeletedByReceiver))
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => (DateTime?)m.SentAt)
                        .FirstOrDefault(),

                    UnreadCount = c.Messages.Count(m =>
                        (m.SenderId == userId ? !m.DeletedBySender : !m.DeletedByReceiver) &&
                        !m.IsRead && m.SenderId != userId)
                })
                .OrderByDescending(x => x.LastMessageTime)
                .ToList();
        }

        private static bool IsConversationDeletedForUser(Conversation conversation, int userId)
        {
            return (conversation.User1Id == userId && conversation.User1Deleted)
                || (conversation.User2Id == userId && conversation.User2Deleted);
        }
    }
}
