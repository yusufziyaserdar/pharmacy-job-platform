using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmacyJobPlatform.Domain.Entities
{
    public class Conversation
    {
        public int Id { get; set; }

        public int User1Id { get; set; }
        public User User1 { get; set; }

        public int User2Id { get; set; }
        public User User2 { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndedAt { get; set; }

        public int? EndedByUserId { get; set; }

        public bool User1Deleted { get; set; } = false;

        public bool User2Deleted { get; set; } = false;

        public ICollection<Message> Messages { get; set; }
    }
}
