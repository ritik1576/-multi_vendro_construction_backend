using System.Text.Json.Serialization;

public class CreateOrderDto
{
    public long UserId { get; set; }

    [JsonPropertyName("user_id")]
    public long UserIdSnakeCase { get => UserId; set => UserId = value; }

    public long AddressId { get; set; }

    [JsonPropertyName("address_id")]
    public long AddressIdSnakeCase { get => AddressId; set => AddressId = value; }

    public string? CouponCode { get; set; }

    [JsonPropertyName("coupon_code")]
    public string? CouponCodeSnakeCase { get => CouponCode; set => CouponCode = value; }

    public string? PaymentMethod { get; set; } = "razorpay";

    [JsonPropertyName("payment_method")]
    public string? PaymentMethodSnakeCase { get => PaymentMethod; set => PaymentMethod = value; }

    public List<CreateOrderItemDto> Items { get; set; } = new();

    [JsonPropertyName("items")]
    public List<CreateOrderItemDto> ItemsSnakeCase { get => Items; set => Items = value; }
}