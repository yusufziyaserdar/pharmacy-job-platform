using PharmacyJobPlatform.Web.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace PharmacyJobPlatform.Web.Models.Auth
{
    public class RegisterViewModel
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$", ErrorMessage = "Şifre en az 8 karakter olmalı; büyük harf, küçük harf, rakam ve noktalama işareti içermelidir")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Şifre tekrarı zorunludur")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Telefon numarası zorunludur")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Telefon numarası 10 haneli olmalıdır")]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsEmailVisible { get; set; } = true;

        public bool IsPhoneNumberVisible { get; set; } = true;
        public string? About { get; set; }
        public IFormFile? ProfileImage { get; set; }

        public string? PharmacyName { get; set; }

        // 🔥 BURASI ÇOK ÖNEMLİ
        public AddressInputViewModel Address { get; set; } = new();

        public List<WorkExperienceInputModel> WorkExperiences { get; set; } = new();

        [Required]
        public string Role { get; set; }
    }
}
