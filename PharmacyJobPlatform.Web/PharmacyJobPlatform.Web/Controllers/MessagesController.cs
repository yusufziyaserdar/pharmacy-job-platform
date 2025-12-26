using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Index()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var conversations = _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .OrderByDescending(c => c.Messages.Max(m => m.SentAt))
                .ToList();

            return View(conversations);
        }

        [Authorize]
        public async Task<IActionResult> Chat(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var conv = await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    (c.User1Id == userId || c.User2Id == userId));

            if (conv == null)
                return Unauthorized();

            // 🔥 OKUNMAMIŞ MESAJLARI OKUNDU YAP
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
        public async Task<IActionResult> Send(int conversationId, string content)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var conv = await _context.Conversations
                .FirstOrDefaultAsync(x =>
                    x.Id == conversationId &&
                    (x.User1Id == userId || x.User2Id == userId));

            if (conv == null)
                return Unauthorized();

            var msg = new Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                Content = content
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                msg.Content,
                msg.SentAt
            });
        }

        public IActionResult Inbox()
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var conversations = _context.Conversations
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .Select(c => new InboxConversationViewModel
                {
                    ConversationId = c.Id,

                    OtherUserId = c.User1Id == userId
                        ? c.User2Id
                        : c.User1Id,

                    OtherUserFullName = c.User1Id == userId
                        ? c.User2.FirstName + " " + c.User2.LastName
                        : c.User1.FirstName + " " + c.User1.LastName,

                    LastMessage = _context.Messages
                        .Where(m => m.ConversationId == c.Id)
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.Content)
                        .FirstOrDefault(),

                    LastMessageTime = _context.Messages
                        .Where(m => m.ConversationId == c.Id)
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.SentAt)
                        .FirstOrDefault(),

                    UnreadCount = _context.Messages
                        .Count(m =>
                            m.ConversationId == c.Id &&
                            !m.IsRead &&
                            m.SenderId != userId)
                })
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();

            return View(conversations);
        }


    }



}
