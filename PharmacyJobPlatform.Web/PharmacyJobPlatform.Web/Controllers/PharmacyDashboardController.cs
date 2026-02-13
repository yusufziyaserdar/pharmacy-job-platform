using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Infrastructure.Data;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Authorize(Roles = "PharmacyOwner")]
    public class PharmacyDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PharmacyDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var myJobPosts = _context.JobPosts
                .Where(j => j.PharmacyOwnerId == userId && !j.IsDeleted)
                .ToList();

            var jobPostIds = myJobPosts.Select(j => j.Id).ToList();

            var applications = _context.JobApplications
                .Include(a => a.JobPost)
                .Where(a => jobPostIds.Contains(a.JobPostId))
                .ToList();

            ViewBag.TotalPosts = myJobPosts.Count;
            ViewBag.ActivePosts = myJobPosts.Count(x => x.IsActive);
            ViewBag.TotalApplications = applications.Count;
            ViewBag.PendingApplications = applications.Count(x => x.Status == Domain.Enums.ApplicationStatus.Pending);

            ViewBag.RecentPosts = myJobPosts
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToList();

            ViewBag.RecentApplications = applications
                .OrderByDescending(x => x.AppliedAt)
                .Take(5)
                .ToList();

            return View();
        }
    }
}
