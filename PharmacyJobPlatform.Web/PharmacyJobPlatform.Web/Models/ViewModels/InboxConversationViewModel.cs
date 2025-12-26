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
    }

}
