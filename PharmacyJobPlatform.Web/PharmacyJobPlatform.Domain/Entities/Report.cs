using PharmacyJobPlatform.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PharmacyJobPlatform.Domain.Entities
{
    public class Report
    {
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string EntityType { get; set; } = null!; // JobPost, User, Comment, Message

        public int EntityId { get; set; }

        [Required]
        public int ReporterUserId { get; set; }
        public User ReporterUser { get; set; } = null!;

        [Required, MaxLength(500)]
        public string Reason { get; set; } = null!;

        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public int? ReviewedByAdminId { get; set; }
        public User? ReviewedByAdmin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }
}
