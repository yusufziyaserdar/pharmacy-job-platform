using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Domain.Enums;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.JobPost;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Authorize(Roles = "Worker,Admin")]
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

            var baseQuery = _context.JobPosts
                .Where(x => x.IsActive)
                .AsQueryable();

            filter.Cities = baseQuery
                .Select(x => x.Address.City)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            filter.Districts = baseQuery
                .Where(x => string.IsNullOrEmpty(filter.City) || x.Address.City == filter.City)
                .Select(x => x.Address.District)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            filter.Neighborhoods = baseQuery
                .Where(x =>
                    (string.IsNullOrEmpty(filter.City) || x.Address.City == filter.City) &&
                    (string.IsNullOrEmpty(filter.District) || x.Address.District == filter.District))
                .Select(x => x.Address.Neighborhood)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var query = baseQuery;

            if (!string.IsNullOrEmpty(filter.City))
                query = query.Where(x => x.Address.City == filter.City);

            if (!string.IsNullOrEmpty(filter.District))
                query = query.Where(x => x.Address.District == filter.District);

            if (!string.IsNullOrEmpty(filter.Neighborhood))
                query = query.Where(x => x.Address.Neighborhood == filter.Neighborhood);

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.Title, $"%{keyword}%") ||
                    EF.Functions.Like(x.Description, $"%{keyword}%"));
            }

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
                    City = x.Address.City,
                    District = x.Address.District,
                    Neighborhood = x.Address.Neighborhood,
                    JobType = x.JobType,
                    DailyWage = x.DailyWage,
                    MonthlySalary = x.MonthlySalary,
                    AlreadyApplied = _context.JobApplications
                        .Any(a => a.JobPostId == x.Id && a.WorkerId == workerId)
                })

                .ToList();

            return View(filter);
        }

        public IActionResult Details(int id)
        {
            var workerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var post = _context.JobPosts
                .Include(x => x.Address)
                .Where(x => x.IsActive && x.Id == id)
                .Select(x => new JobPostDetailsViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    JobType = x.JobType,
                    City = x.Address.City,
                    District = x.Address.District,
                    Neighborhood = x.Address.Neighborhood,
                    DailyWage = x.DailyWage,
                    WorkDate = x.WorkDate,
                    MonthlySalary = x.MonthlySalary,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    AlreadyApplied = _context.JobApplications
                        .Any(a => a.JobPostId == x.Id && a.WorkerId == workerId)
                })
                .FirstOrDefault();

            if (post == null)
                return NotFound();

            return View(post);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteByAdmin(int id)
        {
            var post = await _context.JobPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (post == null)
            {
                TempData["Error"] = "İlan bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            post.IsDeleted = true;
            post.IsActive = false;
            post.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "İlan admin tarafından kaldırıldı.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int jobPostId)
        {
            if (User.IsInRole("Admin"))
            {
                TempData["Error"] = "Admin hesabı ile başvuru yapılamaz.";
                return RedirectToAction(nameof(Index));
            }

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
