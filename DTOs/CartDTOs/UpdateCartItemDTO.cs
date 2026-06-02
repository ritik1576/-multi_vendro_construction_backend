namespace MultiVendorAPI.DTOs.CartDTOs
{
    public class UpdateCartItemDto
    {
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int userId { get; set; }

        public int CartId { get; set; }
    }
}
