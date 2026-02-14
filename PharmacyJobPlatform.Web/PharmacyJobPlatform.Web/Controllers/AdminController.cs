using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Domain.Enums;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.Admin;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private const string SystemUserEmail = "system@pharmacyjobplatform.local";
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var pendingReportsRaw = await _context.Reports
                .AsNoTracking()
                .Where(r => r.Status == ReportStatus.Pending)
                .Include(r => r.ReporterUser)
                .OrderByDescending(r => r.CreatedAt)
                .Take(200)
                .ToListAsync();

            var reviewedReportsRaw = await _context.Reports
                .AsNoTracking()
                .Where(r => r.Status != ReportStatus.Pending)
                .Include(r => r.ReporterUser)
                .Include(r => r.ReviewedByAdmin)
                .OrderByDescending(r => r.ReviewedAt)
                .Take(200)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalJobPosts = await _context.JobPosts.CountAsync(),
                TotalComments = await _context.ProfileComments.CountAsync(),
                TotalReports = await _context.Reports.CountAsync(),
                Users = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(50)
                    .Select(u => new AdminUserItemViewModel
                    {
                        Id = u.Id,
                        FullName = u.FirstName + " " + u.LastName,
                        Email = u.Email,
                        RoleName = u.Role.Name,
                        IsDeleted = u.IsDeleted,
                        CreatedAt = u.CreatedAt
                    })
                    .ToListAsync(),
                JobPosts = await _context.JobPosts
                    .AsNoTracking()
                    .Include(p => p.PharmacyOwner)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(50)
                    .Select(p => new AdminJobPostItemViewModel
                    {
                        Id = p.Id,
                        Title = p.Title,
                        OwnerName = p.PharmacyOwner.FirstName + " " + p.PharmacyOwner.LastName,
                        IsActive = p.IsActive,
                        IsDeleted = p.IsDeleted,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync(),
                Comments = await _context.ProfileComments
                    .AsNoTracking()
                    .Include(c => c.AuthorUser)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(100)
                    .Select(c => new AdminCommentItemViewModel
                    {
                        Id = c.Id,
                        ProfileUserId = c.ProfileUserId,
                        AuthorName = c.IsAnonymous ? "Anonim" : c.AuthorUser.FirstName + " " + c.AuthorUser.LastName,
                        Content = c.Content,
                        IsDeleted = c.IsDeleted,
                        CreatedAt = c.CreatedAt
                    })
                    .ToListAsync(),
                PendingReports = pendingReportsRaw
                    .Select(r => MapReport(r, includeReviewedBy: false))
                    .ToList(),
                ReviewedReports = reviewedReportsRaw
                    .Select(r => MapReport(r, includeReviewedBy: true))
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReportRemove(int reportId)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null || report.Status != ReportStatus.Pending)
            {
                TempData["Error"] = "Rapor bulunamadı ya da zaten işlenmiş.";
                return RedirectToAction(nameof(Index));
            }

            var offenderId = await RemoveReportedContentAsync(report.EntityType, report.EntityId);
            if (!offenderId.HasValue)
            {
                TempData["Error"] = "Raporlanan içerik bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            report.Status = ReportStatus.ResolvedRemoved;
            report.ReviewedByAdminId = adminId;
            report.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Rapor işlendi, içerik kaldırıldı.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReportRemoveWithWarning(int reportId)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null || report.Status != ReportStatus.Pending)
            {
                TempData["Error"] = "Rapor bulunamadı ya da zaten işlenmiş.";
                return RedirectToAction(nameof(Index));
            }

            var offenderId = await RemoveReportedContentAsync(report.EntityType, report.EntityId);
            if (!offenderId.HasValue)
            {
                TempData["Error"] = "Raporlanan içerik bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            report.Status = ReportStatus.ResolvedRemoved;
            report.ReviewedByAdminId = adminId;
            report.ReviewedAt = DateTime.UtcNow;

            if (offenderId.Value != adminId)
            {
                await SendSystemWarningAsync(offenderId.Value);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Rapor işlendi, içerik kaldırıldı ve kullanıcıya uyarı gönderildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReportKeep(int reportId)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null || report.Status != ReportStatus.Pending)
            {
                TempData["Error"] = "Rapor bulunamadı ya da zaten işlenmiş.";
                return RedirectToAction(nameof(Index));
            }

            report.Status = ReportStatus.ResolvedKept;
            report.ReviewedByAdminId = adminId;
            report.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Rapor incelendi, içerik yayında bırakıldı.";
            return RedirectToAction(nameof(Index));
        }

        private AdminReportItemViewModel MapReport(Report report, bool includeReviewedBy)
        {
            return new AdminReportItemViewModel
            {
                Id = report.Id,
                EntityType = report.EntityType,
                EntityId = report.EntityId,
                ReporterName = report.ReporterUser.FirstName + " " + report.ReporterUser.LastName,
                Reason = report.Reason,
                ReportedContent = GetReportedContentPreview(report.EntityType, report.EntityId),
                ContentLinkController = GetContentLinkController(report.EntityType),
                ContentLinkAction = GetContentLinkAction(report.EntityType),
                ContentLinkId = GetContentLinkId(report.EntityType, report.EntityId),
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                ReviewedAt = report.ReviewedAt,
                ReviewedByAdminName = includeReviewedBy && report.ReviewedByAdmin != null
                    ? report.ReviewedByAdmin.FirstName + " " + report.ReviewedByAdmin.LastName
                    : null
            };
        }

        private async Task<int?> RemoveReportedContentAsync(string entityType, int entityId)
        {
            switch (entityType)
            {
                case "JobPost":
                {
                    var post = await _context.JobPosts.FirstOrDefaultAsync(p => p.Id == entityId);
                    if (post == null) return null;
                    post.IsDeleted = true;
                    post.IsActive = false;
                    post.DeletedAt = DateTime.UtcNow;
                    return post.PharmacyOwnerId;
                }
                case "User":
                {
                    var user = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == entityId);
                    if (user == null || user.Role.Name == "Admin") return null;
                    user.IsDeleted = true;
                    user.DeletedAt = DateTime.UtcNow;
                    return user.Id;
                }
                case "Comment":
                {
                    var comment = await _context.ProfileComments.FirstOrDefaultAsync(c => c.Id == entityId);
                    if (comment == null) return null;
                    comment.IsDeleted = true;
                    comment.DeletedAt = DateTime.UtcNow;
                    return comment.AuthorUserId;
                }
                case "Message":
                {
                    var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == entityId);
                    if (message == null) return null;
                    var senderId = message.SenderId;
                    _context.Messages.Remove(message);
                    return senderId;
                }
                default:
                    return null;
            }
        }


        private string GetReportedContentPreview(string entityType, int entityId)
        {
            return entityType switch
            {
                "JobPost" => _context.JobPosts
                    .Where(p => p.Id == entityId)
                    .Select(p => p.Title)
                    .FirstOrDefault() ?? "(İçerik bulunamadı)",
                "User" => _context.Users
                    .Where(u => u.Id == entityId)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault() ?? "(İçerik bulunamadı)",
                "Comment" => _context.ProfileComments
                    .Where(c => c.Id == entityId)
                    .Select(c => c.Content)
                    .FirstOrDefault() ?? "(İçerik bulunamadı)",
                "Message" => _context.Messages
                    .Where(m => m.Id == entityId)
                    .Select(m => m.Content)
                    .FirstOrDefault() ?? "(İçerik bulunamadı)",
                _ => "(Desteklenmeyen içerik türü)"
            };
        }

        private string? GetContentLinkController(string entityType) => entityType switch
        {
            "JobPost" => "Jobs",
            "User" => "Profile",
            "Comment" => "Profile",
            _ => null
        };

        private string? GetContentLinkAction(string entityType) => entityType switch
        {
            "JobPost" => "Details",
            "User" => "Index",
            "Comment" => "Index",
            _ => null
        };

        private int? GetContentLinkId(string entityType, int entityId)
        {
            if (entityType == "Comment")
            {
                return _context.ProfileComments
                    .Where(c => c.Id == entityId)
                    .Select(c => (int?)c.ProfileUserId)
                    .FirstOrDefault();
            }

            return entityType is "JobPost" or "User" ? entityId : null;
        }

        private async Task SendSystemWarningAsync(int targetUserId)
        {
            var systemUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == SystemUserEmail);

            if (systemUser == null)
            {
                return;
            }

            var conversation = await _context.Conversations.FirstOrDefaultAsync(c =>
                (c.User1Id == systemUser.Id && c.User2Id == targetUserId) ||
                (c.User1Id == targetUserId && c.User2Id == systemUser.Id));

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    User1Id = systemUser.Id,
                    User2Id = targetUserId
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            var warningText = "[Sistem Uyarısı] Uygunsuz hareket ettiğiniz tespit edildi. Lütfen topluluk kurallarına uygun davranınız. İhlallerin devam etmesi halinde profiliniz geçici veya kalıcı olarak yasaklanabilir.";

            _context.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                SenderId = systemUser.Id,
                Content = warningText,
                SentAt = DateTime.UtcNow,
                IsRead = false
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJobPost(int id)
        {
            var post = await _context.JobPosts.FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                TempData["Error"] = "İlan bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            post.IsDeleted = true;
            post.DeletedAt = DateTime.UtcNow;
            post.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["Success"] = "İlan kaldırıldı.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreJobPost(int id)
        {
            var post = await _context.JobPosts.FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                TempData["Error"] = "İlan bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            post.IsDeleted = false;
            post.DeletedAt = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "İlan geri alındı.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleJobPostStatus(int id)
        {
            var post = await _context.JobPosts.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
            {
                TempData["Error"] = "İlan bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            if (post.IsDeleted)
            {
                TempData["Error"] = "Kaldırılmış ilan için durum değiştirilemez. Önce ilanı geri alın.";
                return RedirectToAction(nameof(Index));
            }

            post.IsActive = !post.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = post.IsActive ? "İlan tekrar aktifleştirildi." : "İlan pasife alındı.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var allComments = await _context.ProfileComments
                .AsNoTracking()
                .Select(c => new { c.Id, c.ParentCommentId })
                .ToListAsync();

            if (!allComments.Any(c => c.Id == id))
            {
                TempData["Error"] = "Yorum bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var ids = GetCommentTreeIds(id, allComments.Select(c => (c.Id, c.ParentCommentId)).ToList());
            var utcNow = DateTime.UtcNow;

            var comments = await _context.ProfileComments
                .Where(c => ids.Contains(c.Id))
                .ToListAsync();

            foreach (var comment in comments)
            {
                comment.IsDeleted = true;
                comment.DeletedAt = utcNow;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Yorum ve yanıtları kaldırıldı.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreComment(int id)
        {
            var allComments = await _context.ProfileComments
                .AsNoTracking()
                .Select(c => new { c.Id, c.ParentCommentId })
                .ToListAsync();

            if (!allComments.Any(c => c.Id == id))
            {
                TempData["Error"] = "Yorum bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var ids = GetCommentTreeIds(id, allComments.Select(c => (c.Id, c.ParentCommentId)).ToList());

            var comments = await _context.ProfileComments
                .Where(c => ids.Contains(c.Id))
                .ToListAsync();

            foreach (var comment in comments)
            {
                comment.IsDeleted = false;
                comment.DeletedAt = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Yorum ve yanıtları geri alındı.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (currentAdminId == id)
            {
                TempData["Error"] = "Kendi hesabınızı admin panelinden kaldıramazsınız.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (user.Role.Name == "Admin")
            {
                TempData["Error"] = "Başka bir admin hesabı bu panelden kaldırılamaz.";
                return RedirectToAction(nameof(Index));
            }

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Kullanıcı profili kaldırıldı.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            user.IsDeleted = false;
            user.DeletedAt = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Kullanıcı profili geri alındı.";
            return RedirectToAction(nameof(Index));
        }

        private static HashSet<int> GetCommentTreeIds(int rootId, List<(int Id, int? ParentCommentId)> allComments)
        {
            var ids = new HashSet<int> { rootId };
            var queue = new Queue<int>();
            queue.Enqueue(rootId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var children = allComments
                    .Where(c => c.ParentCommentId == current)
                    .Select(c => c.Id)
                    .ToList();

                foreach (var childId in children)
                {
                    if (ids.Add(childId))
                    {
                        queue.Enqueue(childId);
                    }
                }
            }

            return ids;
        }
    }
}
