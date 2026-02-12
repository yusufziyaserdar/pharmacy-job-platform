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
            var deletedRows = await _context.JobPosts.Where(p => p.Id == id).ExecuteDeleteAsync();

            TempData[deletedRows > 0 ? "Success" : "Error"] = deletedRows > 0
                ? "İlan kaldırıldı."
                : "İlan bulunamadı.";

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

            var toDelete = new HashSet<int> { id };
            var queue = new Queue<int>();
            queue.Enqueue(id);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var children = allComments
                    .Where(c => c.ParentCommentId == current)
                    .Select(c => c.Id)
                    .ToList();

                foreach (var childId in children)
                {
                    if (toDelete.Add(childId))
                    {
                        queue.Enqueue(childId);
                    }
                }
            }

            await _context.ProfileComments
                .Where(c => toDelete.Contains(c.Id))
                .ExecuteDeleteAsync();

            TempData["Success"] = "Yorum ve yanıtları silindi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (currentAdminId == id)
            {
                TempData["Error"] = "Kendi hesabınızı admin panelinden silemezsiniz.";
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
                TempData["Error"] = "Başka bir admin hesabı bu panelden silinemez.";
                return RedirectToAction(nameof(Index));
            }

            await _context.UserRatings
                .Where(r => r.RatedUserId == id || r.RaterId == id)
                .ExecuteDeleteAsync();

            var allComments = await _context.ProfileComments
                .AsNoTracking()
                .Select(c => new { c.Id, c.ParentCommentId, c.ProfileUserId, c.AuthorUserId })
                .ToListAsync();

            var baseCommentIds = allComments
                .Where(c => c.ProfileUserId == id || c.AuthorUserId == id)
                .Select(c => c.Id)
                .ToList();

            var commentsToDelete = new HashSet<int>(baseCommentIds);
            var commentQueue = new Queue<int>(baseCommentIds);

            while (commentQueue.Count > 0)
            {
                var current = commentQueue.Dequeue();
                var childIds = allComments
                    .Where(c => c.ParentCommentId == current)
                    .Select(c => c.Id)
                    .ToList();

                foreach (var childId in childIds)
                {
                    if (commentsToDelete.Add(childId))
                    {
                        commentQueue.Enqueue(childId);
                    }
                }
            }

            if (commentsToDelete.Count > 0)
            {
                await _context.ProfileComments
                    .Where(c => commentsToDelete.Contains(c.Id))
                    .ExecuteDeleteAsync();
            }

            await _context.ConversationRequests
                .Where(r => r.FromUserId == id || r.ToUserId == id)
                .ExecuteDeleteAsync();

            await _context.Conversations
                .Where(c => c.User1Id == id || c.User2Id == id)
                .ExecuteDeleteAsync();

            await _context.JobApplications
                .Where(a => a.WorkerId == id)
                .ExecuteDeleteAsync();

            await _context.JobPosts
                .Where(p => p.PharmacyOwnerId == id)
                .ExecuteDeleteAsync();

            await _context.Users
                .Where(u => u.Id == id)
                .ExecuteDeleteAsync();

            TempData["Success"] = "Kullanıcı ve ilişkili verileri silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
