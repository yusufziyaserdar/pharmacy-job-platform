using PharmacyJobPlatform.Web.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace PharmacyJobPlatform.Web.Models.Profile
{
    public class ProfileEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        public bool IsEmailVisible { get; set; }

        public bool IsPhoneNumberVisible { get; set; }

        public string? About { get; set; }

        public string? PharmacyName { get; set; }

        public AddressInputViewModel Address { get; set; } = new();

        public string? ExistingProfileImagePath { get; set; }
        public IFormFile? ProfileImage { get; set; }

        public List<WorkExperienceEditModel> WorkExperiences { get; set; }
            = new();
    }

    public class WorkExperienceEditModel
    {
        public int? Id { get; set; }
        public string PharmacyName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
