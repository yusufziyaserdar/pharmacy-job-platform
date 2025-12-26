using System.ComponentModel.DataAnnotations;

namespace PharmacyJobPlatform.Domain.Entities
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; }

        [Required]
        public int SenderId { get; set; }
        public User Sender { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}
