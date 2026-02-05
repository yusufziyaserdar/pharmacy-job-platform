using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Domain.Enums;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.JobPost;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Authorize(Roles = "Worker")]
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(JobPostFilterViewModel filter)
        {
            var workerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var query = _context.JobPosts
                .Where(x => x.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.City))
                query = query.Where(x => x.Address.City == filter.City);

            if (!string.IsNullOrEmpty(filter.District))
                query = query.Where(x => x.Address.District == filter.District);

            if (!string.IsNullOrEmpty(filter.Neighborhood))
                query = query.Where(x => x.Address.Neighborhood == filter.Neighborhood);


            if (filter.JobType.HasValue)
                query = query.Where(x => x.JobType == filter.JobType);

            if (filter.MinWage.HasValue)
            {
                query = query.Where(x =>
                    x.JobType == JobType.Daily && x.DailyWage >= filter.MinWage ||
                    x.JobType == JobType.Permanent && x.MonthlySalary >= filter.MinWage
                );
            }

            if (filter.MaxWage.HasValue)
            {
                query = query.Where(x =>
                    x.JobType == JobType.Daily && x.DailyWage <= filter.MaxWage ||
                    x.JobType == JobType.Permanent && x.MonthlySalary <= filter.MaxWage
                );
            }

            filter.Results = query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new JobPostListItemViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    City=x.Address.City,
                    JobType = x.JobType,
                    DailyWage = x.DailyWage,
                    MonthlySalary = x.MonthlySalary,
                    AlreadyApplied = _context.JobApplications
                        .Any(a => a.JobPostId == x.Id && a.WorkerId == workerId)
                })

                .ToList();

            return View(filter);
        }

        // 👇 BAŞVURU ACTION
        [HttpPost]
        public async Task<IActionResult> Apply(int jobPostId)
        {
            var workerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var exists = _context.JobApplications
                .Any(x => x.JobPostId == jobPostId && x.WorkerId == workerId);

            if (exists)
                return RedirectToAction("Index");

            _context.JobApplications.Add(new JobApplication
            {
                JobPostId = jobPostId,
                WorkerId = workerId
            });

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}