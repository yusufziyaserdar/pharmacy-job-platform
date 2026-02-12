namespace PharmacyJobPlatform.Web.Models.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalJobPosts { get; set; }
        public int TotalComments { get; set; }

        public List<AdminUserItemViewModel> Users { get; set; } = new();
        public List<AdminJobPostItemViewModel> JobPosts { get; set; } = new();
        public List<AdminCommentItemViewModel> Comments { get; set; } = new();
    }

    public class AdminUserItemViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminJobPostItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminCommentItemViewModel
    {
        public int Id { get; set; }
        public int ProfileUserId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
