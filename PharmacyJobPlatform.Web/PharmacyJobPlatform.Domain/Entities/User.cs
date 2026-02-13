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

        public string? PharmacyName { get; set; }

        public string? ProfileImagePath { get; set; }
        public string? About { get; set; }

        public bool EmailConfirmed { get; set; }
        public string? EmailConfirmationToken { get; set; }
        public DateTime? EmailConfirmationTokenExpiresAt { get; set; }

        public int? AddressId { get; set; }
        public Address? Address { get; set; }

        [Required]
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<WorkExperience> WorkExperiences { get; set; }
            = new List<WorkExperience>();

        public ICollection<UserRating> RatingsGiven { get; set; }
            = new List<UserRating>();

        public ICollection<UserRating> RatingsReceived { get; set; }
            = new List<UserRating>();

        public ICollection<ProfileComment> CommentsReceived { get; set; }
            = new List<ProfileComment>();

        public ICollection<ProfileComment> CommentsWritten { get; set; }
            = new List<ProfileComment>();
    }
}
