using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.Auth;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;

namespace PharmacyJobPlatform.Web.Controllers
{

    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ---------------- LOGIN ----------------
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var test = model;
            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Email veya şifre hatalı");
                return View(model);
            }

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            // 🎯 Role bazlı yönlendirme
            return user.Role.Name switch
            {
                "PharmacyOwner" => RedirectToAction("Index", "PharmacyDashboard"),
                _ => RedirectToAction("Index", "WorkerDashboard")
            };
        }

        // ---------------- REGISTER ----------------
        public IActionResult Register()
        {
            SetGoogleMapsApiKey();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Role == "Worker")
            {
                RemoveModelStateByPrefix("Address");
            }

            if (model.Role == "PharmacyOwner")
            {
                RemoveModelStateByPrefix("WorkExperiences");
            }

            if (!ModelState.IsValid)
            {
                SetGoogleMapsApiKey();
                return View(model);
            }

            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("", "Bu email zaten kayıtlı");
                SetGoogleMapsApiKey();
                return View(model);
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == model.Role);

            if (role == null)
            {
                ModelState.AddModelError("", "Geçersiz rol seçimi");
                SetGoogleMapsApiKey();
                return View(model);
            }

            // ============================
            // 📸 Profil Fotoğrafı
            // ============================
            string? profileImagePath = null;

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot", "images", "profiles");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.ProfileImage.CopyToAsync(stream);

                profileImagePath = "/images/profiles/" + fileName;
            }

            // ============================
            // 🏠 Address (SADECE PharmacyOwner)
            // ============================
            Address? address = null;

            if (model.Role == "PharmacyOwner")
            {
                if (model.Address == null)
                {
                    ModelState.AddModelError("", "Eczane sahibi için adres bilgisi zorunludur");
                    SetGoogleMapsApiKey();
                    return View(model);
                }

                address = new Address
                {
                    City = model.Address.City,
                    District = model.Address.District,
                    Neighborhood = model.Address.Neighborhood,
                    Street = model.Address.Street,
                    BuildingNumber = model.Address.BuildingNumber,
                    Description = model.Address.Description
                };

                _context.Addresses.Add(address);
            }

            // ============================
            // 👤 User
            // ============================
            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                PhoneNumber = model.PhoneNumber,
                About = model.About,
                ProfileImagePath = profileImagePath,
                RoleId = role.Id,
                Address = address, // 🔥 EF otomatik AddressId set eder
                CreatedAt = DateTime.UtcNow
            };

            // ============================
            // 🏥 Work Experiences (SADECE Worker)
            // ============================
            if (model.Role == "Worker" && model.WorkExperiences != null)
            {
                foreach (var exp in model.WorkExperiences)
                {
                    if (string.IsNullOrWhiteSpace(exp.PharmacyName) || !exp.StartDate.HasValue)
                        continue;

                    user.WorkExperiences.Add(new WorkExperience
                    {
                        PharmacyName = exp.PharmacyName,
                        StartDate = exp.StartDate.Value,
                        EndDate = exp.EndDate
                    });
                }
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
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

        // ---------------- LOGOUT ----------------
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();

        private void SetGoogleMapsApiKey()
        {
            ViewData["GoogleMapsApiKey"] = _configuration["GoogleMaps:ApiKey"];
        }
    }

}
