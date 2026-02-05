using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.Profile;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Route("Profile")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
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

            return View(user);
        }

        [HttpGet("Edit")]
        public IActionResult Edit()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var user = _context.Users
                .Include(u => u.WorkExperiences)
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
                ExistingProfileImagePath = user.ProfileImagePath,
                WorkExperiences = user.WorkExperiences.Select(x => new WorkExperienceEditModel
                {
                    Id = x.Id,
                    PharmacyName = x.PharmacyName,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate
                }).ToList()

            };

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
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.About = model.About;
            user.Address.City = model.Address.City;
            user.Address.District = model.Address.District;
            user.Address.Neighborhood = model.Address.Neighborhood;
            user.Address.Street = model.Address.Street;
            user.Address.BuildingNumber = model.Address.BuildingNumber;
            user.Address.Description = model.Address.Description;


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



    }

}