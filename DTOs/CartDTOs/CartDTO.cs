namespace MultiVendorAPI.DTOs.CartDTOs
{
    public class CartDto
    {
        public long UserId { get; set; } = 0;

        public List<CartItemDto> Items { get; set; } = new();

        public decimal? TotalPrice { get; set; }


    }
}
