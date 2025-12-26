using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Domain.Enums;
using PharmacyJobPlatform.Infrastructure.Data;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Authorize(Roles = "PharmacyOwner")]
    public class JobPostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobPostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult MyPosts()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var posts = _context.JobPosts
                .Where(x => x.PharmacyOwnerId == userId)
                .Select(x => new JobPostListViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    JobType = x.JobType,
                    City = x.City,
                    DailyWage = x.JobType == JobType.Daily ? x.DailyWage : null,
                    MonthlySalary = x.JobType == JobType.Permanent ? x.MonthlySalary : null,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return View(posts);
        }


        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(JobPostCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var post = new JobPost
            {
                PharmacyOwnerId = userId,
                Title = model.Title,
                Description = model.Description,
                JobType = model.JobType,
                City = model.City,
                Address = model.Address,
                DailyWage = model.JobType == JobType.Daily ? model.DailyWage : null,
                WorkDate = model.JobType == JobType.Daily ? model.WorkDate : null,
                MonthlySalary = model.JobType == JobType.Permanent ? model.MonthlySalary : null
            };

            _context.JobPosts.Add(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyPosts));
        }
    }


}
