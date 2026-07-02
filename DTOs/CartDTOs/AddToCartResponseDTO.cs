namespace MultiVendorAPI.DTOs.CartDTOs
{
    public class AddToCartResponseDto
    {
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public long userId { get; set; }

        public long CartItemId { get; set; }

        public long CartId { get; set; }

    }
}
