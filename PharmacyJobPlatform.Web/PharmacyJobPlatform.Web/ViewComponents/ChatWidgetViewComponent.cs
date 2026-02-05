using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.ViewModels;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.ViewComponents
{
    public class ChatWidgetViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public ChatWidgetViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return View(new List<ChatWidgetConversationVM>());

            int userId = int.Parse(
                UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            );

            var conversations = await _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .Select(c => new ChatWidgetConversationVM
                {
                    ConversationId = c.Id,
                    OtherUserName =
                        c.User1Id == userId
                            ? c.User2.FirstName + " " + c.User2.LastName
                            : c.User1.FirstName + " " + c.User1.LastName,
                    LastMessage = c.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.Content)
                        .FirstOrDefault(),
                    UnreadCount = c.Messages.Count(m =>
                        !m.IsRead && m.SenderId != userId)
                })
                .ToListAsync();

            return View(conversations);
        }
    }
}