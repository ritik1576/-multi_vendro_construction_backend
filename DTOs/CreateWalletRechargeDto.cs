using System.Text.Json.Serialization;

namespace InframartAPI_New.DTOs
{
    public class CreateWalletRechargeDto
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }
}
