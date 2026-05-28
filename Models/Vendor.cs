namespace InframartAPI_New.Models
{
    public class Vendor
    {
        public int Id { get; set; }

        public string BusinessName { get; set; } = string.Empty;

        public string GST { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string BankDetails { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public int UserId { get; set; }

        public User? User { get; set; }
    }
}