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

            return View(conversations);
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
