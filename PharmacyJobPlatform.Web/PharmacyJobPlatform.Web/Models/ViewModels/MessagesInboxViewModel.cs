using System;
using System.Collections.Generic;

namespace PharmacyJobPlatform.Web.Models.ViewModels
{
    public class MessagesInboxViewModel
    {
        public List<InboxConversationViewModel> Conversations { get; set; } = new();

        public List<ConversationRequestViewModel> IncomingRequests { get; set; } = new();
    }

    public class ConversationRequestViewModel
    {
        public int Id { get; set; }

        public int FromUserId { get; set; }

        public string FromUserName { get; set; } = string.Empty;

        public string? FromUserProfileImagePath { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}