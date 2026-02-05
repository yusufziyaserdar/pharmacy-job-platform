namespace PharmacyJobPlatform.Web.Models.ViewModels
{
    public class WorkerRecentApplicationVM
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; }
        public string CompanyName { get; set; }

        public DateTime AppliedAt { get; set; }
        public string Status { get; set; } // Pending / Accepted / Rejected
    }
}
