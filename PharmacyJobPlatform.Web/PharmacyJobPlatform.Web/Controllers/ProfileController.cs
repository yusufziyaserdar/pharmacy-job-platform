using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Domain.Enums;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.Profile;
using PharmacyJobPlatform.Web.Models.ViewModels;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Route("Profile")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
        }

        [HttpGet("{id}")]
        public IActionResult Index(int id)
        {
            var user = _context.Users
                .Include(u => u.Role)
                .Include(u => u.WorkExperiences)
                .FirstOrDefault(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return NotFound();

            int? viewerId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                viewerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            }

            int? conversationId = null;
            bool canSendRequest = false;
            bool hasPendingOutgoingRequest = false;
            int? incomingRequestId = null;
            bool canRateUser = false;
            int? existingRating = null;

            var ratingQuery = _context.UserRatings.Where(r => r.RatedUserId == user.Id);
            var ratingCount = ratingQuery.Count();
            decimal? averageRating = ratingCount > 0
                ? Math.Round(ratingQuery.Average(r => (decimal)r.Stars), 1)
                : null;

            if (viewerId.HasValue && viewerId.Value != user.Id)
            {
                conversationId = _context.Conversations
                    .Where(c => (c.User1Id == viewerId && c.User2Id == user.Id) ||
                                (c.User1Id == user.Id && c.User2Id == viewerId))
                    .Where(c =>
                        !c.EndedAt.HasValue &&
                        ((c.User1Id == viewerId && !c.User1Deleted) ||
                         (c.User2Id == viewerId && !c.User2Deleted)))
                    .Select(c => (int?)c.Id)
                    .FirstOrDefault();

                if (!conversationId.HasValue)
                {
                    hasPendingOutgoingRequest = _context.ConversationRequests.Any(r =>
                        r.FromUserId == viewerId &&
                        r.ToUserId == user.Id &&
                        !r.IsAccepted);

                    incomingRequestId = _context.ConversationRequests
                        .Where(r => r.FromUserId == user.Id &&
                                    r.ToUserId == viewerId &&
                                    !r.IsAccepted)
                        .Select(r => (int?)r.Id)
                        .FirstOrDefault();

                    canSendRequest = !hasPendingOutgoingRequest && !incomingRequestId.HasValue;
                }

                canRateUser = _context.JobApplications.Any(ja =>
                    ja.Status == ApplicationStatus.Accepted &&
                    ((ja.WorkerId == viewerId.Value && ja.JobPost.PharmacyOwnerId == user.Id)
                    || (ja.WorkerId == user.Id && ja.JobPost.PharmacyOwnerId == viewerId.Value)));

                existingRating = _context.UserRatings
                    .Where(r => r.RaterId == viewerId.Value && r.RatedUserId == user.Id)
                    .Select(r => (int?)r.Stars)
                    .FirstOrDefault();
            }

            var vm = new ProfileDetailViewModel
            {
                User = user,
                ConversationId = conversationId,
                CanSendRequest = canSendRequest,
                HasPendingOutgoingRequest = hasPendingOutgoingRequest,
                IncomingRequestId = incomingRequestId,
                AverageRating = averageRating,
                RatingCount = ratingCount,
                CanRateUser = canRateUser,
                ExistingRating = existingRating,
                Comments = GetProfileComments(user.Id)
            };

            return View(vm);
        }

        [HttpGet("Edit")]
        public IActionResult Edit()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var user = _context.Users
                .Include(u => u.WorkExperiences)
                .Include(u => u.Role)
                .Include(u => u.Address)
                .FirstOrDefault(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
                return NotFound();

            var vm = new ProfileEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsEmailVisible = user.IsEmailVisible,
                IsPhoneNumberVisible = user.IsPhoneNumberVisible,
                IsCvVisible = user.IsCvVisible,
                About = user.About,
                PharmacyName = user.PharmacyName,
                Address = user.Address == null
                    ? new AddressInputViewModel()
                    : new AddressInputViewModel
                    {
                        City = user.Address.City,
                        District = user.Address.District,
                        Neighborhood = user.Address.Neighborhood,
                        Street = user.Address.Street,
                        BuildingNumber = user.Address.BuildingNumber,
                        Description = user.Address.Description
                    },
                ExistingProfileImagePath = user.ProfileImagePath,
                ExistingCvFilePath = user.CvFilePath,
                WorkExperiences = user.WorkExperiences.Select(x => new WorkExperienceEditModel
                {
                    Id = x.Id,
                    PharmacyName = x.PharmacyName,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate
                }).ToList()

            };

            ViewData["IsPharmacyOwner"] = user.Role?.Name == "PharmacyOwner";
            ViewData["GoogleMapsApiKey"] = _configuration["GoogleMaps:ApiKey"];
            return View(vm);
        }

        [HttpPost("Edit")]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (userId != model.Id)
                return Forbid();

            var user = _context.Users
                .Include(u => u.WorkExperiences)
                .Include(u => u.Role)
                .Include(u => u.Address)
                .FirstOrDefault(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
                return NotFound();

            if (user.Role?.Name != "PharmacyOwner")
            {
                RemoveModelStateByPrefix("Address");
            }

            if (user.Role?.Name == "PharmacyOwner" && string.IsNullOrWhiteSpace(model.PharmacyName))
            {
                ModelState.AddModelError(nameof(model.PharmacyName), "Eczane adı zorunludur");
            }

            if (!ModelState.IsValid)
            {
                ViewData["IsPharmacyOwner"] = user.Role?.Name == "PharmacyOwner";
                ViewData["GoogleMapsApiKey"] = _configuration["GoogleMaps:ApiKey"];
                return View(model);
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.IsEmailVisible = model.IsEmailVisible;
            user.IsPhoneNumberVisible = model.IsPhoneNumberVisible;
            user.IsCvVisible = model.IsCvVisible;
            user.About = model.About;
            if (user.Role?.Name == "PharmacyOwner")
            {
                user.PharmacyName = model.PharmacyName;
                user.Address ??= new Address();
                user.Address.City = model.Address.City;
                user.Address.District = model.Address.District;
                user.Address.Neighborhood = model.Address.Neighborhood;
                user.Address.Street = model.Address.Street;
                user.Address.BuildingNumber = model.Address.BuildingNumber;
                user.Address.Description = model.Address.Description;
            }


            // 🖼 Profil Foto
            if (model.ProfileImage != null)
            {
                var fileName = $"user-{user.Id}{Path.GetExtension(model.ProfileImage.FileName)}";
                var path = Path.Combine("wwwroot/images/profiles", fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await model.ProfileImage.CopyToAsync(stream);

                user.ProfileImagePath = "/images/profiles/" + fileName;
            }

            if (model.CvFile != null)
            {
                var uploadsFolder = Path.Combine("wwwroot", "files", "cvs");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"user-{user.Id}{Path.GetExtension(model.CvFile.FileName)}";
                var path = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await model.CvFile.CopyToAsync(stream);

                user.CvFilePath = "/files/cvs/" + fileName;
            }

            if (user.Role?.Name != "PharmacyOwner")
            {
                // 🏥 Work Experiences (basit versiyon)
                _context.WorkExperiences.RemoveRange(user.WorkExperiences);

                foreach (var exp in model.WorkExperiences)
                {
                    user.WorkExperiences.Add(new WorkExperience
                    {
                        PharmacyName = exp.PharmacyName,
                        StartDate = exp.StartDate,
                        EndDate = exp.EndDate
                    });
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { id = user.Id });
        }

        [HttpGet("DownloadCv/{id}")]
        public IActionResult DownloadCv(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id && !u.IsDeleted);
            if (user == null || string.IsNullOrWhiteSpace(user.CvFilePath))
            {
                return NotFound();
            }

            var viewerId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;
            var isOwner = viewerId == id.ToString();
            if (!user.IsCvVisible && !isOwner)
            {
                return Forbid();
            }

            var webRootPath = _environment.WebRootPath;
            var normalizedCvPath = user.CvFilePath.Replace('\\', '/');
            var relativePath = normalizedCvPath.TrimStart('/');

            var fullPath = Path.IsPathRooted(user.CvFilePath)
                ? user.CvFilePath
                : normalizedCvPath.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase)
                    ? Path.GetFullPath(Path.Combine(_environment.ContentRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)))
                    : Path.GetFullPath(Path.Combine(webRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var extension = Path.GetExtension(fullPath);
            var downloadName = $"cv-{user.FirstName}-{user.LastName}{extension}";
            return PhysicalFile(fullPath, "application/octet-stream", downloadName);
        }

        [Authorize]
        [HttpPost("Rate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int ratedUserId, int stars)
        {
            var raterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (raterId == ratedUserId)
            {
                TempData["Error"] = "Kendi profilinize puan veremezsiniz.";
                return RedirectToAction("Index", new { id = ratedUserId });
            }

            if (stars < 1 || stars > 5)
            {
                TempData["Error"] = "Puan 1 ile 5 arasında olmalıdır.";
                return RedirectToAction("Index", new { id = ratedUserId });
            }

            var ratedUserExists = await _context.Users.AnyAsync(u => u.Id == ratedUserId && !u.IsDeleted);
            if (!ratedUserExists)
            {
                return NotFound();
            }

            var workedTogether = await _context.JobApplications.AnyAsync(ja =>
                ja.Status == ApplicationStatus.Accepted &&
                ((ja.WorkerId == raterId && ja.JobPost.PharmacyOwnerId == ratedUserId)
                || (ja.WorkerId == ratedUserId && ja.JobPost.PharmacyOwnerId == raterId)));

            if (!workedTogether)
            {
                TempData["Error"] = "Sadece birlikte çalıştığınız kullanıcılara puan verebilirsiniz.";
                return RedirectToAction("Index", new { id = ratedUserId });
            }

            var existingRating = await _context.UserRatings
                .FirstOrDefaultAsync(r => r.RaterId == raterId && r.RatedUserId == ratedUserId);

            if (existingRating == null)
            {
                _context.UserRatings.Add(new UserRating
                {
                    RaterId = raterId,
                    RatedUserId = ratedUserId,
                    Stars = stars
                });
            }
            else
            {
                existingRating.Stars = stars;
                existingRating.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Değerlendirmeniz kaydedildi.";

            return RedirectToAction("Index", new { id = ratedUserId });
        }

        [Authorize]
        [HttpPost("Comment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(int profileUserId, string content, bool isAnonymous)
        {
            var authorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (!await _context.Users.AnyAsync(u => u.Id == profileUserId && !u.IsDeleted))
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Yorum metni boş olamaz.";
                return RedirectToAction("Index", new { id = profileUserId });
            }

            _context.ProfileComments.Add(new ProfileComment
            {
                ProfileUserId = profileUserId,
                AuthorUserId = authorId,
                Content = content.Trim(),
                IsAnonymous = isAnonymous
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Yorumunuz paylaşıldı.";

            return RedirectToAction("Index", new { id = profileUserId });
        }

        [Authorize]
        [HttpPost("Reply")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int profileUserId, int parentCommentId, string content, bool isAnonymous)
        {
            var authorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var parentComment = await _context.ProfileComments
                .FirstOrDefaultAsync(c => c.Id == parentCommentId && c.ProfileUserId == profileUserId && !c.IsDeleted);

            if (parentComment == null)
            {
                TempData["Error"] = "Yanıtlanacak yorum bulunamadı.";
                return RedirectToAction("Index", new { id = profileUserId });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Yanıt metni boş olamaz.";
                return RedirectToAction("Index", new { id = profileUserId });
            }

            _context.ProfileComments.Add(new ProfileComment
            {
                ProfileUserId = profileUserId,
                AuthorUserId = authorId,
                ParentCommentId = parentCommentId,
                Content = content.Trim(),
                IsAnonymous = isAnonymous
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Yanıtınız paylaşıldı.";

            return RedirectToAction("Index", new { id = profileUserId });
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("DeleteComment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id, int profileUserId)
        {
            var allComments = await _context.ProfileComments
                .AsNoTracking()
                .Select(c => new { c.Id, c.ParentCommentId })
                .ToListAsync();

            if (!allComments.Any(c => c.Id == id))
            {
                TempData["Error"] = "Yorum bulunamadı.";
                return RedirectToAction("Index", new { id = profileUserId });
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
            TempData["Success"] = "Yorum kaldırıldı.";
            return RedirectToAction("Index", new { id = profileUserId });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Role?.Name == "Admin")
            {
                TempData["Error"] = "Admin profili bu ekrandan kaldırılamaz.";
                return RedirectToAction("Index", new { id });
            }

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profil kaldırıldı.";
            return RedirectToAction("Index", "Jobs");
        }

        private List<ProfileCommentItemViewModel> GetProfileComments(int profileUserId)
        {
            var comments = _context.ProfileComments
                .AsNoTracking()
                .Where(c => c.ProfileUserId == profileUserId && !c.IsDeleted)
                .Include(c => c.AuthorUser)
                .OrderBy(c => c.CreatedAt)
                .ToList();

            var commentLookup = comments
                .Select(c => new ProfileCommentItemViewModel
                {
                    Id = c.Id,
                    ProfileUserId = c.ProfileUserId,
                    AuthorUserId = c.AuthorUserId,
                    AuthorDisplayName = c.IsAnonymous
                        ? "Anonim"
                        : $"{c.AuthorUser.FirstName} {c.AuthorUser.LastName}",
                    IsAnonymous = c.IsAnonymous,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .ToDictionary(c => c.Id);

            var roots = new List<ProfileCommentItemViewModel>();

            foreach (var comment in comments)
            {
                var vm = commentLookup[comment.Id];

                if (comment.ParentCommentId.HasValue && commentLookup.TryGetValue(comment.ParentCommentId.Value, out var parent))
                {
                    parent.Replies.Add(vm);
                }
                else
                {
                    roots.Add(vm);
                }
            }

            return roots;
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

        private void RemoveModelStateByPrefix(string prefix)
        {
            var keysToRemove = ModelState.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }
        }

    }

}
