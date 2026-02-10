using System.ComponentModel.DataAnnotations;

namespace PharmacyJobPlatform.Domain.Entities
{
    public class UserRating
    {
        public int Id { get; set; }

        [Required]
        public int RaterId { get; set; }
        public User Rater { get; set; } = null!;

        [Required]
        public int RatedUserId { get; set; }
        public User RatedUser { get; set; } = null!;

        [Range(1, 5)]
        public int Stars { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
