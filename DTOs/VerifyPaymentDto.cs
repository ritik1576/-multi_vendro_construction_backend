namespace InframartAPI_New.DTOs
{
    public class VerifyPaymentDto
    {
        public long Order_Id { get; set; }

        public string RazorpayPayment_Id { get; set; } = string.Empty;

        public string RazorpayOrder_Id { get; set; } = string.Empty;

        public string RazorpaySignature { get; set; } = string.Empty;
    }
}