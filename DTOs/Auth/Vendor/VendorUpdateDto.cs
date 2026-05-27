namespace InframartAPI_New.DTOs.Vendor
{
    public class VendorUpdateDto
    {
        public int Id { get; set; }

        public string? BusinessName { get; set; }

        public string? GST { get; set; }

        public string? Address { get; set; }

        public string? BankDetails { get; set; }
    }
}