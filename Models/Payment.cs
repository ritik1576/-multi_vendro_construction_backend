namespace InframartAPI_New.Models
{
    public class Payment
    {
        public long Id { get; set; }
        public long Order_Id { get; set; }

        public long? User_Id { get; set; }
        public string? Payment_Method { get; set; }
        public string? Transaction_Id { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; } = "created";

        public DateTime? Paid_At { get; set; }

        public DateTime Created_At { get; set; }

        public string? Razorpay_Payment_Id { get; set; }
        public string? Razorpay_Order_Id { get; set; }
    }
}