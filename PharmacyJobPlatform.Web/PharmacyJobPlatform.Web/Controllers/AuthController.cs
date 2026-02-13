using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PharmacyJobPlatform.Infrastructure.Data;
using PharmacyJobPlatform.Web.Models.Auth;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Web.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

namespace PharmacyJobPlatform.Web.Controllers
{

    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailSender? emailSender = null)
        {
            _context = context;
            _configuration = configuration;
            _emailSender = emailSender ?? new NullEmailSender();
        }

        // ---------------- LOGIN ----------------
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Email == model.Email && !u.IsDeleted);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Email veya şifre hatalı");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError("", "Email adresinizi doğrulamadan giriş yapamazsınız. Mailinizi kontrol edin.");
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
                "Admin" => RedirectToAction("Index", "Admin"),
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

            if (_context.Users.Any(u => u.Email == model.Email && !u.IsDeleted))
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
                if (string.IsNullOrWhiteSpace(model.PharmacyName))
                {
                    ModelState.AddModelError(nameof(model.PharmacyName), "Eczane adı zorunludur");
                }

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

            if (!ModelState.IsValid)
            {
                SetGoogleMapsApiKey();
                return View(model);
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
                PharmacyName = model.PharmacyName,
                RoleId = role.Id,
                Address = address, // 🔥 EF otomatik AddressId set eder
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = false,
                EmailConfirmationToken = GenerateEmailToken(),
                EmailConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(24)
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

            if (_emailSender == null || _emailSender is NullEmailSender)
            {
                ModelState.AddModelError("", "Email servisi yapılandırılmadığı için doğrulama maili gönderilemedi.");
                SetGoogleMapsApiKey();
                return View(model);
            }

            await SendConfirmationEmailAsync(user);
            TempData["AuthMessage"] = "Kayıt tamamlandı. Giriş yapmadan önce email adresinizi doğrulayın.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                ViewData["ConfirmMessage"] = "Doğrulama bilgileri eksik.";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
            if (user == null)
            {
                ViewData["ConfirmMessage"] = "Kullanıcı bulunamadı.";
                return View();
            }

            if (user.EmailConfirmed)
            {
                ViewData["ConfirmMessage"] = "Email adresiniz zaten doğrulanmış.";
                return View();
            }

            if (user.EmailConfirmationTokenExpiresAt == null ||
                user.EmailConfirmationTokenExpiresAt < DateTime.UtcNow ||
                user.EmailConfirmationToken != token)
            {
                ViewData["ConfirmMessage"] = "Doğrulama bağlantısı geçersiz veya süresi dolmuş.";
                return View();
            }

            user.EmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiresAt = null;
            await _context.SaveChangesAsync();

            ViewData["ConfirmMessage"] = "Email adresiniz doğrulandı. Artık giriş yapabilirsiniz.";
            return View();
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


        private static string GenerateEmailToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            return WebEncoders.Base64UrlEncode(tokenBytes);
        }

        private async Task SendConfirmationEmailAsync(User user)
        {
            if (_emailSender == null)
            {
                return;
            }

            var confirmationUrl = Url.Action(
                "ConfirmEmail",
                "Auth",
                new { email = user.Email, token = user.EmailConfirmationToken },
                Request.Scheme);

            var body = $@"
                <p>Merhaba {user.FirstName},</p>
                <p>Kayıt işlemini tamamlamak için aşağıdaki bağlantıya tıklayın:</p>
                <p><a href=""{confirmationUrl}"">Email adresimi doğrula</a></p>
                <p>Bağlantı 24 saat boyunca geçerlidir.</p>";

            await _emailSender.SendEmailAsync(user.Email, "Email Doğrulama", body);
        }
    }

}
