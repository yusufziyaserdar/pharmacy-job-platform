namespace PharmacyJobPlatform.Web.Models.ViewModels
{
    public class ChatWidgetConversationVM
    {
        public int ConversationId { get; set; }

        public int OtherUserId {  get; set; }
        public string OtherUserName { get; set; }
        public string? LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
}
