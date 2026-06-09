using MultiVendorAPI.Models;

namespace MultiVendorAPI.Repositories.Interfaces
{
    public interface ICartRepository
    {
        Task<Cart?> GetByUserIdWithItemsAsync(long userId);

        Task<Product?> GetProductByNameAsync(string productName);

        Task AddCartAsync(Cart cart);

        Task AddCartItemAsync(CartItem cartItem);

        Task UpdateCartItemAsync(CartItem cartItem);

        Task RemoveCartItemAsync(CartItem cartItem);

        Task RemoveCartItemsAsync(IEnumerable<CartItem> cartItems);

        Task SaveChangesAsync();

        public Task<CartItem?> GetCartItemByIdAsync(long cartItemId);
    }
}
