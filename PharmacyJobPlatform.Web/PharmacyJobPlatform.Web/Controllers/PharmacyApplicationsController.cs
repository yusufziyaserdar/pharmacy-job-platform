using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Domain.Enums;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.JobApplication;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Authorize(Roles = "PharmacyOwner")]
    public class PharmacyApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PharmacyApplicationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 📥 İlan başvuruları
        public IActionResult JobApplications(int jobPostId)
        {
            var ownerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Güvenlik: İlan bana mı ait?
            var job = _context.JobPosts
                .FirstOrDefault(j => j.Id == jobPostId && j.PharmacyOwnerId == ownerId && !j.IsDeleted);

            if (job == null)
                return Unauthorized();

            var applications = _context.JobApplications
                .Include(a => a.Worker)
                .Where(a => a.JobPostId == jobPostId)
                .OrderByDescending(a => a.AppliedAt)
                .Select(a => new PharmacyApplicationViewModel
                {
                    ApplicationId = a.Id,
                    JobTitle = job.Title,
                    WorkerId = a.WorkerId,
                    WorkerFullName = a.Worker.FirstName + " " + a.Worker.LastName,
                    WorkerEmail = a.Worker.Email,
                    Status = a.Status,
                    AppliedAt = a.AppliedAt
                })
                .ToList();

            ViewBag.JobTitle = job.Title;

            return View(applications);
        }

        [HttpPost]
        public async Task<IActionResult> Accept(int id)
        {
            var app = await _context.JobApplications
                .Include(x => x.JobPost)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (app == null)
                return NotFound();

            app.Status = ApplicationStatus.Accepted;

            var existingConversation = await _context.Conversations
                .FirstOrDefaultAsync(c =>
                    !c.EndedAt.HasValue &&
                    ((c.User1Id == app.WorkerId && c.User2Id == app.JobPost.PharmacyOwnerId) ||
                     (c.User1Id == app.JobPost.PharmacyOwnerId && c.User2Id == app.WorkerId)));

            if (existingConversation == null)
            {
                _context.Conversations.Add(new Conversation
                {
                    User1Id = app.WorkerId,
                    User2Id = app.JobPost.PharmacyOwnerId
                });
            }
            else
            {
                existingConversation.User1Deleted = false;
                existingConversation.User2Deleted = false;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("JobApplications", new { jobPostId = app.JobPostId });
        }


        // ❌ Reddet
        [HttpPost]
        public IActionResult Reject(int id)
        {
            var app = _context.JobApplications
                .Include(a => a.JobPost)
                .FirstOrDefault(a => a.Id == id);

            if (app == null)
                return NotFound();

            app.Status = ApplicationStatus.Rejected;
            _context.SaveChanges();

            return RedirectToAction("JobApplications", new { jobPostId = app.JobPostId });
        }
    }
}
