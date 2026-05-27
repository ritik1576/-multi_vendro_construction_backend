namespace InframartAPI_New.DTOs.Vendor
{
    public class VendorRegisterDto
    {
        public string? BusinessName { get; set; }
        public string? GST { get; set; }
        public string? Address { get; set; }
        public string? BankDetails { get; set; }
        public int UserId { get; set; }
    }
}