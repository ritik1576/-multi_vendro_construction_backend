using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Data;
using MultiVendorAPI.Models;
using MultiVendorAPI.Repositories.Interfaces;

namespace MultiVendorAPI.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;

        public CartRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Cart?> GetByUserIdWithItemsAsync(long userId)
        {
            return _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public Task<Product?> GetProductByNameAsync(string productName)
        {
            return _context.Products
                .FirstOrDefaultAsync(p => p.Name != null && p.Name.ToLower() == productName.ToLower());
        }

        public async Task AddCartAsync(Cart cart)
        {
            await _context.Carts.AddAsync(cart);
        }

        public async Task AddCartItemAsync(CartItem cartItem)
        {
            await _context.CartItems.AddAsync(cartItem);
        }

        public Task UpdateCartItemAsync(CartItem cartItem)
        {
            _context.CartItems.Update(cartItem);
            return Task.CompletedTask;
        }

        public Task RemoveCartItemAsync(CartItem cartItem)
        {
            _context.CartItems.Remove(cartItem);
            return Task.CompletedTask;
        }

        public Task RemoveCartItemsAsync(IEnumerable<CartItem> cartItems)
        {
            _context.CartItems.RemoveRange(cartItems);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
        public async Task<CartItem?> GetCartItemByIdAsync(long cartItemId)
        {
            return await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);
        }
    }
}
