using PharmacyJobPlatform.Domain.Entities;

namespace PharmacyJobPlatform.Web.Models.Profile
{
    public class ProfileDetailViewModel
    {
        public User User { get; set; } = null!;

        public int? ConversationId { get; set; }

        public bool CanSendRequest { get; set; }

        public bool HasPendingOutgoingRequest { get; set; }

        public int? IncomingRequestId { get; set; }

        public decimal? AverageRating { get; set; }

        public int RatingCount { get; set; }

        public bool CanRateUser { get; set; }

        public int? ExistingRating { get; set; }
    }
}
