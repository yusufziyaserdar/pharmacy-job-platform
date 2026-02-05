using System.ComponentModel.DataAnnotations;

namespace PharmacyJobPlatform.Domain.Entities
{
    public class Address
    {
        public int Id { get; set; }

        [Required]
        public string City { get; set; } = null!;

        [Required]
        public string District { get; set; } = null!;

        [Required]
        public string Neighborhood { get; set; } = null!;

        public string? Street { get; set; }
        public string? BuildingNumber { get; set; }
        public string? Description { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();
    }
}
