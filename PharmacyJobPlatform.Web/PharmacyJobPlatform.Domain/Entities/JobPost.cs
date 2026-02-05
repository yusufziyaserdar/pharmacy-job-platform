using PharmacyJobPlatform.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PharmacyJobPlatform.Domain.Entities
{
    public class JobPost
    {
        public int Id { get; set; }

        [Required]
        public int PharmacyOwnerId { get; set; }
        public User PharmacyOwner { get; set; }

        [Required]
        public JobType JobType { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public int AddressId { get; set; }
        public Address Address { get; set; }

        // Günlük iş
        public decimal? DailyWage { get; set; }
        public DateTime? WorkDate { get; set; }

        // Kalıcı iş
        public decimal? MonthlySalary { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
