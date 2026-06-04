namespace MultiVendorAPI.DTOs.CartDTOs
{
    public class CartItemDto
    {
        public int CartItemId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal? Price { get; set; }

        public int Quantity { get; set; }

        public decimal? TotalPrice => Price * Quantity;
    }
}
