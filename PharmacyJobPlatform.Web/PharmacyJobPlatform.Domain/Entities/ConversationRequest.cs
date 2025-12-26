namespace PharmacyJobPlatform.Domain.Entities
{
    public class ConversationRequest
    {
        public int Id { get; set; }

        public int FromUserId { get; set; }
        public User FromUser { get; set; }

        public int ToUserId { get; set; }
        public User ToUser { get; set; }

        public bool IsAccepted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
