namespace MultiVendorAPI.Models
{
    public class Cart
    {
        public int Id { get; set; }

        public long UserId { get; set; } = 0;

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
