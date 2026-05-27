namespace InframartAPI_New.Models
{
    public class Vendor
    {
        public int Id { get; set; }
        public string? BusinessName { get; set; }
        public string? GST { get; set; }
        public string? Address { get; set; }
        public string? BankDetails { get; set; }

        public string Status { get; set; } = "Pending";

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}