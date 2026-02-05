using PharmacyJobPlatform.Domain.Entities;


namespace PharmacyJobPlatform.Web.Models.ViewModels
{
    public class InboxConversationViewModel
    {
        public int ConversationId { get; set; }

        public int OtherUserId { get; set; }
        public string OtherUserFullName { get; set; }

        public string LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }

        public int UnreadCount { get; set; }

        public List<MessageViewModel> Messages { get; set; } = new();
    }

    public class MessageViewModel
    {
        public int SenderId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
    }
}