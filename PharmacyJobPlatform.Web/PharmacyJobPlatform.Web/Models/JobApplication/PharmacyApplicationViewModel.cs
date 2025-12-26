using PharmacyJobPlatform.Domain.Enums;
using PharmacyJobPlatform.Domain.Entities;

namespace PharmacyJobPlatform.Web.Models.JobApplication
{
    public class PharmacyApplicationViewModel
    {
        public int ApplicationId { get; set; }

        public string JobTitle { get; set; }

        public int WorkerId { get; set; }
        public string WorkerFullName { get; set; }
        public string WorkerEmail { get; set; }

        public ApplicationStatus Status { get; set; }
        public DateTime AppliedAt { get; set; }
    }
}
