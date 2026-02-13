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
    [Authorize(Roles = "PharmacyOwner")]
    public class JobPostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobPostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =======================
        // MY POSTS
        // =======================
        public IActionResult MyPosts()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var posts = _context.JobPosts
                .Include(x => x.Address)
                .Where(x => x.PharmacyOwnerId == userId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new JobPostListViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    JobType = x.JobType,
                    City = x.Address.City,
                    Description = x.Description,
                    DailyWage = x.JobType == JobType.Daily ? x.DailyWage : null,
                    MonthlySalary = x.JobType == JobType.Permanent ? x.MonthlySalary : null,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToList();

            return View(posts);
        }

        // =======================
        // CREATE (GET)
        // =======================
        public IActionResult Create()
        {
            return View(new JobPostCreateViewModel());
        }

        // =======================
        // CREATE (POST)
        // =======================
        [HttpPost]
        public async Task<IActionResult> Create(JobPostCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);

            if (user == null || user.AddressId == null)
            {
                ModelState.AddModelError("", "İş ilanı oluşturabilmek için adres bilgisi zorunludur.");
                return View(model);
            }

            var post = new JobPost
            {
                PharmacyOwnerId = userId,
                Title = model.Title,
                Description = model.Description,
                JobType = model.JobType,
                AddressId = user.AddressId.Value,
                DailyWage = model.JobType == JobType.Daily ? model.DailyWage : null,
                WorkDate = model.JobType == JobType.Daily ? model.WorkDate : null,
                MonthlySalary = model.JobType == JobType.Permanent ? model.MonthlySalary : null
            };

            _context.JobPosts.Add(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyPosts));
        }

        // =======================
        // DETAILS
        // =======================
        public IActionResult Details(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var post = _context.JobPosts
                .Include(x => x.Address)
                .Where(x => x.Id == id && x.PharmacyOwnerId == userId && !x.IsDeleted)
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
                    CreatedAt = x.CreatedAt
                })
                .FirstOrDefault();

            if (post == null)
                return NotFound();

            return View(post);
        }

        // =======================
        // EDIT (GET)
        // =======================
        public IActionResult Edit(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var post = _context.JobPosts
                .FirstOrDefault(x => x.Id == id && x.PharmacyOwnerId == userId && !x.IsDeleted);

            if (post == null)
                return NotFound();

            var model = new JobPostEditViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                JobType = post.JobType,
                DailyWage = post.DailyWage,
                WorkDate = post.WorkDate,
                MonthlySalary = post.MonthlySalary,
                IsActive = post.IsActive
            };

            return View(model);
        }

        // =======================
        // EDIT (POST)
        // =======================
        [HttpPost]
        public async Task<IActionResult> Edit(JobPostEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var post = await _context.JobPosts
                .FirstOrDefaultAsync(x => x.Id == model.Id && x.PharmacyOwnerId == userId && !x.IsDeleted);

            if (post == null)
                return NotFound();

            post.Title = model.Title;
            post.Description = model.Description;
            post.JobType = model.JobType;
            post.IsActive = model.IsActive;

            post.DailyWage = model.JobType == JobType.Daily ? model.DailyWage : null;
            post.WorkDate = model.JobType == JobType.Daily ? model.WorkDate : null;
            post.MonthlySalary = model.JobType == JobType.Permanent ? model.MonthlySalary : null;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyPosts));
        }
    }
}
