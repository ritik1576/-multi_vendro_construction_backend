namespace MultiVendorAPI.DTOs.CartDTOs
{
    public class AddToCartDto
    {
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int userId { get; set; }

    }
}
