using System.ComponentModel.DataAnnotations;

namespace PharmacyJobPlatform.Domain.Entities
{
    public class ProfileComment
    {
        public int Id { get; set; }

        [Required]
        public int ProfileUserId { get; set; }
        public User ProfileUser { get; set; } = null!;

        [Required]
        public int AuthorUserId { get; set; }
        public User AuthorUser { get; set; } = null!;

        public int? ParentCommentId { get; set; }
        public ProfileComment? ParentComment { get; set; }

        public ICollection<ProfileComment> Replies { get; set; } = new List<ProfileComment>();

        [Required, MaxLength(500)]
        public string Content { get; set; } = null!;

        public bool IsAnonymous { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
