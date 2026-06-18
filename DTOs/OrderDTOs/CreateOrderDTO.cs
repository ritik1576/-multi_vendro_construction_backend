public class CreateOrderDto
{
    public long UserId { get; set; }

    public long AddressId { get; set; }

    public string? CouponCode { get; set; }

    public List<CreateOrderItemDto> Items { get; set; } = new();
}