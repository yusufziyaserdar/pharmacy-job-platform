using PharmacyJobPlatform.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PharmacyJobPlatform.Web.Models.JobPost
{
    public class JobPostCreateViewModel
    {
        [Required, MaxLength(150)]
        public string Title { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        [Required]
        public JobType JobType { get; set; }

        public decimal? DailyWage { get; set; }
        public DateTime? WorkDate { get; set; }

        public decimal? MonthlySalary { get; set; }
    }
}