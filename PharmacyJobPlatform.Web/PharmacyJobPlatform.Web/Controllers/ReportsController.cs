using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Infrastructure.Data;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "JobPost", "User", "Comment", "Message"
        };

        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string entityType, int entityId, string reason, string? returnController = null, string? returnAction = null, int? returnId = null)
        {
            var reporterUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (!SupportedTypes.Contains(entityType))
            {
                TempData["Error"] = "Geçersiz rapor türü.";
                return RedirectBack(returnController, returnAction, returnId);
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Rapor nedeni boş olamaz.";
                return RedirectBack(returnController, returnAction, returnId);
            }

            var exists = await EntityExistsAsync(entityType, entityId);
            if (!exists)
            {
                TempData["Error"] = "Raporlanacak içerik bulunamadı.";
                return RedirectBack(returnController, returnAction, returnId);
            }

            var duplicatePending = await _context.Reports.AnyAsync(r =>
                r.ReporterUserId == reporterUserId &&
                r.EntityType == entityType &&
                r.EntityId == entityId &&
                r.Status == Domain.Enums.ReportStatus.Pending);

            if (duplicatePending)
            {
                TempData["Error"] = "Bu içerik için zaten bekleyen bir raporunuz var.";
                return RedirectBack(returnController, returnAction, returnId);
            }

            _context.Reports.Add(new Report
            {
                EntityType = entityType,
                EntityId = entityId,
                ReporterUserId = reporterUserId,
                Reason = reason.Trim()
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Raporunuz alındı. İnceleme sonrası gerekli işlem yapılacaktır.";
            return RedirectBack(returnController, returnAction, returnId);
        }

        private async Task<bool> EntityExistsAsync(string entityType, int entityId)
        {
            return entityType switch
            {
                "JobPost" => await _context.JobPosts.AnyAsync(x => x.Id == entityId),
                "User" => await _context.Users.AnyAsync(x => x.Id == entityId),
                "Comment" => await _context.ProfileComments.AnyAsync(x => x.Id == entityId),
                "Message" => await _context.Messages.AnyAsync(x => x.Id == entityId),
                _ => false
            };
        }

        private IActionResult RedirectBack(string? controller, string? action, int? id)
        {
            controller ??= "Home";
            action ??= "Index";
            if (id.HasValue)
                return RedirectToAction(action, controller, new { id = id.Value });

            return RedirectToAction(action, controller);
        }
    }
}
