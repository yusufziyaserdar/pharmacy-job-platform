using PharmacyJobPlatform.Domain.Enums;

namespace PharmacyJobPlatform.Web.Models.JobPost
{
    public class JobPostListItemViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; }
        public string City { get; set; }
        public string Description { get; set; }

        public JobType JobType { get; set; }

        public decimal? DailyWage { get; set; }
        public decimal? MonthlySalary { get; set; }

        public bool AlreadyApplied { get; set; }
    }
}