namespace InframartAPI_New.DTOs
{
    public class VendorRegisterDto
    {


        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string ShopName { get; set; } = string.Empty;

        public string ShopSlug { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string GstNumber { get; set; } = string.Empty;
    }
}