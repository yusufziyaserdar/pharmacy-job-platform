namespace PharmacyJobPlatform.Web.Models.Profile
{
    public class ProfileCommentItemViewModel
    {
        public int Id { get; set; }

        public int ProfileUserId { get; set; }

        public int AuthorUserId { get; set; }

        public string AuthorDisplayName { get; set; } = null!;

        public bool IsAnonymous { get; set; }

        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public List<ProfileCommentItemViewModel> Replies { get; set; } = new();
    }
}
