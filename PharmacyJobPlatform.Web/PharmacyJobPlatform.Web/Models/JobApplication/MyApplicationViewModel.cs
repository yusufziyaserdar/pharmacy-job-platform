using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Domain.Enums;

namespace PharmacyJobPlatform.Web.Models.JobApplication
{

    public class MyApplicationViewModel
    {
        public int ApplicationId { get; set; }

        public string JobTitle { get; set; }
        public string PharmacyName { get; set; }

        public JobType JobType { get; set; }

        public decimal? DailyWage { get; set; }
        public decimal? MonthlySalary { get; set; }

        public ApplicationStatus Status { get; set; }
        public DateTime AppliedAt { get; set; }
    }

}
