using System.ComponentModel.DataAnnotations;
using PharmacyJobPlatform.Domain.Enums;

namespace PharmacyJobPlatform.Domain.Entities
{
    public class JobApplication
    {
        public int Id { get; set; }

        [Required]
        public int JobPostId { get; set; }
        public JobPost JobPost { get; set; }

        [Required]
        public int WorkerId { get; set; }
        public User Worker { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }

}

