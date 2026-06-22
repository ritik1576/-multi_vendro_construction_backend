public class CreateOrderDto
{
    public long UserId { get; set; }

    public long AddressId { get; set; }

    public string? CouponCode { get; set; }

    public string? PaymentMethod { get; set; } = "razorpay";

    public List<CreateOrderItemDto> Items { get; set; } = new();
}