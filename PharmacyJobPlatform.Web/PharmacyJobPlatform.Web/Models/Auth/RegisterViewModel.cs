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

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        public string PhoneNumber { get; set; }
        public string? About { get; set; }
        public IFormFile? ProfileImage { get; set; }

        // 🔥 BURASI ÇOK ÖNEMLİ
        public AddressInputViewModel Address { get; set; } = new();

        public List<WorkExperienceInputModel> WorkExperiences { get; set; } = new();

        [Required]
        public string Role { get; set; }
    }
}