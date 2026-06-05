using System.ComponentModel.DataAnnotations;

namespace MultiVendorAPI.DTOs.AddressDTOs
{
    public class UpdateAddressDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(255)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address Line 1 is required")]
        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        [StringLength(100)]
        public string State { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Country { get; set; } = "India";

        [Required(ErrorMessage = "Postal code is required")]
        [StringLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [StringLength(20)]
        public string AddressType { get; set; } = "home"; // 'home' or 'office'

        public bool IsDefault { get; set; }
    }
}
