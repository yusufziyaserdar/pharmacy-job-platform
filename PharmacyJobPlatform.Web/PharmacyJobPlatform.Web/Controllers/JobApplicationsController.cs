using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.JobApplication;
using System.Security.Claims;

[Authorize(Roles = "Worker")]
public class JobApplicationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public JobApplicationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Apply(int jobId)
    {
        var workerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var alreadyApplied = _context.JobApplications
            .Any(a => a.JobPostId == jobId && a.WorkerId == workerId);

        if (alreadyApplied)
        {
            TempData["Error"] = "Bu ilana zaten başvurdunuz.";
            return RedirectToAction("Index", "Jobs");
        }

        var application = new JobApplication
        {
            JobPostId = jobId,
            WorkerId = workerId
        };

        _context.JobApplications.Add(application);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Başvurunuz başarıyla gönderildi.";
        return RedirectToAction("MyApplications");
    }

    public IActionResult MyApplications()
    {
        var workerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var applications = _context.JobApplications
            .Include(a => a.JobPost)
                .ThenInclude(j => j.PharmacyOwner)
            .Where(a => a.WorkerId == workerId)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new MyApplicationViewModel
            {
                ApplicationId = a.Id,
                JobTitle = a.JobPost.Title,
                PharmacyName = a.JobPost.PharmacyOwner.FirstName + " " + a.JobPost.PharmacyOwner.LastName,
                JobType = a.JobPost.JobType,
                DailyWage = a.JobPost.DailyWage,
                MonthlySalary = a.JobPost.MonthlySalary,
                Status = a.Status,
                AppliedAt = a.AppliedAt
            })
            .ToList();

        return View(applications);
    }
}
