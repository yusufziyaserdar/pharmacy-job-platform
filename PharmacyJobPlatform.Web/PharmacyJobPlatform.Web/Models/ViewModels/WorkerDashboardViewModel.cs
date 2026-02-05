namespace PharmacyJobPlatform.Web.Models.ViewModels
{
    public class WorkerDashboardViewModel
    {
        public int TotalApplications { get; set; }
        public int ActiveChats { get; set; }
        public int UnreadMessages { get; set; }

        public List<WorkerRecentApplicationVM> RecentApplications { get; set; }
            = new();

        public List<WorkerRecentConversationVM> RecentConversations { get; set; }
            = new();
    }
}
