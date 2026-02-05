using System.ComponentModel.DataAnnotations;

namespace PharmacyJobPlatform.Web.Models.ViewModels
{
    public class AddressInputViewModel
    {
        [Required]
        public string City { get; set; }

        [Required]
        public string District { get; set; }

        [Required]
        public string Neighborhood { get; set; }

        public string? Street { get; set; }
        public string? BuildingNumber { get; set; }
        public string? Description { get; set; }
    }
}