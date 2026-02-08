using PharmacyJobPlatform.Domain.Enums;

namespace PharmacyJobPlatform.Web.Models.JobPost
{
    public class JobPostFilterViewModel
    {
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Neighborhood { get; set; }
        public string? Keyword { get; set; }

        public JobType? JobType { get; set; }

        public decimal? MinWage { get; set; }
        public decimal? MaxWage { get; set; }

        public List<string> Cities { get; set; } = new();
        public List<string> Districts { get; set; } = new();
        public List<string> Neighborhoods { get; set; } = new();

        public List<JobPostListItemViewModel> Results { get; set; } = new();
    }
}
