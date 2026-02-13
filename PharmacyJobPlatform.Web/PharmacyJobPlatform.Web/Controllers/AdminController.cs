using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.Admin;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalJobPosts = await _context.JobPosts.CountAsync(),
                TotalComments = await _context.ProfileComments.CountAsync(),
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
                        IsDeleted=p.IsDeleted,
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
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJobPost(int id)
        {
            var post = await _context.JobPosts

                .FirstOrDefaultAsync(p => p.Id == id);

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
            var post = await _context.JobPosts

                .FirstOrDefaultAsync(p => p.Id == id);

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