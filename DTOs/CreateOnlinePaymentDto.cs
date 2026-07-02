using System.Text.Json.Serialization;

namespace InframartAPI_New.DTOs
{
    public class CreateOnlinePaymentDto
    {
        [JsonPropertyName("cartId")]
        public int CartId { get; set; }

        [JsonPropertyName("addressId")]
        public long? AddressId { get; set; }

        [JsonPropertyName("couponCode")]
        public string? CouponCode { get; set; }
    }
}
