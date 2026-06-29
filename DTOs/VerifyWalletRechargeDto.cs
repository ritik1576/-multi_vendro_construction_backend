using System.Text.Json.Serialization;

namespace InframartAPI_New.DTOs
{
    public class VerifyWalletRechargeDto
    {
        private string _razorpayOrderId = string.Empty;
        private string _razorpayPaymentId = string.Empty;
        private string _razorpaySignature = string.Empty;

        [JsonPropertyName("razorpayOrderId")]
        public string RazorpayOrderId { get => _razorpayOrderId; set => _razorpayOrderId = value; }

        [JsonPropertyName("razorpay_order_id")]
        public string RazorpayOrderIdSnake { get => _razorpayOrderId; set => _razorpayOrderId = value; }

        [JsonPropertyName("razorpayPaymentId")]
        public string RazorpayPaymentId { get => _razorpayPaymentId; set => _razorpayPaymentId = value; }

        [JsonPropertyName("razorpay_payment_id")]
        public string RazorpayPaymentIdSnake { get => _razorpayPaymentId; set => _razorpayPaymentId = value; }

        [JsonPropertyName("razorpaySignature")]
        public string RazorpaySignature { get => _razorpaySignature; set => _razorpaySignature = value; }

        [JsonPropertyName("razorpay_signature")]
        public string RazorpaySignatureSnake { get => _razorpaySignature; set => _razorpaySignature = value; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }
}
