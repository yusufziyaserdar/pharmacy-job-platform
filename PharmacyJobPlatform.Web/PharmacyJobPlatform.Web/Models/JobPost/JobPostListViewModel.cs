using PharmacyJobPlatform.Domain.Enums;

namespace PharmacyJobPlatform.Web.Models.JobPost
{
    public class JobPostListViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;
        public JobType JobType { get; set; }

        public string City { get; set; } = null!;
        public string Description { get; set; } = null!;

        public decimal? DailyWage { get; set; }
        public decimal? MonthlySalary { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}