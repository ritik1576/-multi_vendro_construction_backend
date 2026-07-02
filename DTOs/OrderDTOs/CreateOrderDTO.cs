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

    [JsonPropertyName("payment_type")]
    public string? PaymentTypeSnakeCase { get => PaymentMethod; set => PaymentMethod = value; }

    [JsonPropertyName("paymentType")]
    public string? PaymentTypeCamelCase { get => PaymentMethod; set => PaymentMethod = value; }

    [JsonPropertyName("payment_mode")]
    public string? PaymentModeSnakeCase { get => PaymentMethod; set => PaymentMethod = value; }

    [JsonPropertyName("paymentMode")]
    public string? PaymentModeCamelCase { get => PaymentMethod; set => PaymentMethod = value; }

    [JsonPropertyName("payment")]
    public string? PaymentAlias { get => PaymentMethod; set => PaymentMethod = value; }

    public List<CreateOrderItemDto> Items { get; set; } = new();
}