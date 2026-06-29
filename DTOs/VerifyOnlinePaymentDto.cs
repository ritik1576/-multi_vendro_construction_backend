using System.Text.Json.Serialization;

namespace InframartAPI_New.DTOs
{
    public class VerifyOnlinePaymentDto
    {
        [JsonPropertyName("orderId")]
        public long OrderId { get; set; }

        [JsonPropertyName("razorpayOrderId")]
        public string RazorpayOrderId { get; set; } = string.Empty;

        [JsonPropertyName("razorpayPaymentId")]
        public string RazorpayPaymentId { get; set; } = string.Empty;

        [JsonPropertyName("razorpaySignature")]
        public string RazorpaySignature { get; set; } = string.Empty;
    }
}
