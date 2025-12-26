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

        [Required]
        [Display(Name = "Kayıt Türü")]
        public string Role { get; set; } // Worker | PharmacyOwner
    }
}
