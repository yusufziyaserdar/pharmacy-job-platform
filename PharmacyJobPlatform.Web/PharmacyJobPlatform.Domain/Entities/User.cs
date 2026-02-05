using System.ComponentModel.DataAnnotations;

namespace PharmacyJobPlatform.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        public string PhoneNumber { get; set; } = null!;

        public string? ProfileImagePath { get; set; }
        public string? About { get; set; }

        public int? AddressId { get; set; }
        public Address? Address { get; set; }

        [Required]
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<WorkExperience> WorkExperiences { get; set; }
            = new List<WorkExperience>();
    }
}
