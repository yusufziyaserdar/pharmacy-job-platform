using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmacyJobPlatform.Domain.Entities
{
    public class WorkExperience
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string PharmacyName { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; } // hâlâ çalışıyorsa null
    }

}
