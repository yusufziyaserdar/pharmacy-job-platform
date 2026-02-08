using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.ViewModels;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Authorize(Roles = "Worker")]
    public class WorkerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WorkerDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var model = new WorkerDashboardViewModel
            {
                // 🔹 Başvurularım
                TotalApplications = _context.JobApplications
                    .Count(a => a.WorkerId == userId),

                // 🔹 Aktif sohbetler
                ActiveChats = _context.Conversations
                    .Count(c => c.User1Id == userId || c.User2Id == userId),

                // 🔹 Okunmamış mesajlar
                UnreadMessages = _context.Messages
                    .Include(m => m.Conversation)
                    .Count(m =>
                        !m.IsRead &&
                        m.SenderId != userId &&
                        (m.Conversation.User1Id == userId ||
                         m.Conversation.User2Id == userId)
                    ),

                // 🔹 Son başvurular
                RecentApplications = _context.JobApplications
                    .Include(a => a.JobPost)
                        .ThenInclude(j => j.PharmacyOwner)
                    .Where(a => a.WorkerId == userId)
                    .OrderByDescending(a => a.AppliedAt)
                    .Take(5)
                    .Select(a => new WorkerRecentApplicationVM
                    {
                        JobId = a.JobPostId,
                        JobTitle = a.JobPost.Title,
                        CompanyName =
                            string.IsNullOrWhiteSpace(a.JobPost.PharmacyOwner.PharmacyName)
                                ? a.JobPost.PharmacyOwner.FirstName + " " + a.JobPost.PharmacyOwner.LastName
                                : a.JobPost.PharmacyOwner.PharmacyName,
                        AppliedAt = a.AppliedAt,
                        Status = a.Status.ToString()
                    })
                    .ToList(),

                // 🔹 Son sohbetler
                RecentConversations = _context.Conversations
                    .Include(c => c.Messages)
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .Where(c => c.User1Id == userId || c.User2Id == userId)
                    .OrderByDescending(c =>
                        c.Messages.OrderByDescending(m => m.SentAt)
                                  .Select(m => m.SentAt)
                                  .FirstOrDefault())
                    .Take(5)
                    .Select(c => new WorkerRecentConversationVM
                    {
                        ConversationId = c.Id,
                        OtherUserId = c.User1Id == userId ? c.User2Id : c.User1Id,
                        OtherUserName = c.User1Id == userId
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
                        UnreadCount = c.Messages
                            .Count(m => !m.IsRead && m.SenderId != userId)
                    })
                    .ToList()
            };

            return View(model);
        }
    }
}
