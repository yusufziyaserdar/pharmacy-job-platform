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

        public ProfileController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("{id}")]
        public IActionResult Index(int id)
        {
            var user = _context.Users
                .Include(u => u.Role)
                .Include(u => u.WorkExperiences)
                .FirstOrDefault(u => u.Id == id);

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
                ExistingRating = existingRating
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
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return NotFound();

            var vm = new ProfileEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
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
                .FirstOrDefault(u => u.Id == userId);

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

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { id = user.Id });
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

            var ratedUserExists = await _context.Users.AnyAsync(u => u.Id == ratedUserId);
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
