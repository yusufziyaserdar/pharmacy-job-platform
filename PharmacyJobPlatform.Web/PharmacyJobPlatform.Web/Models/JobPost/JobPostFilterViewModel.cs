using PharmacyJobPlatform.Domain.Enums;

namespace PharmacyJobPlatform.Web.Models.JobPost
{
    public class JobPostFilterViewModel
    {
        public string? City { get; set; }
        public JobType? JobType { get; set; }

        public decimal? MinWage { get; set; }
        public decimal? MaxWage { get; set; }

        public List<JobPostListItemViewModel> Results { get; set; } = new();
    }
}
