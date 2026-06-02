namespace MultiVendorAPI.DTOs.CartDTOs
{
    public class CartDto
    {
        public string UserId { get; set; } = string.Empty;

        public List<CartItemDto> Items { get; set; } = new();

        public decimal? TotalPrice { get; set; }
    }
}
