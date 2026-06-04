using MultiVendorAPI.Models;
using MultiVendorAPI.Data;
using Microsoft.EntityFrameworkCore;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task CreateOrderAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task CreateOrderItemAsync(OrderItem orderItem)
    {
        await _context.OrderItems.AddAsync(orderItem);
    }

    public async Task<Product?> GetProductByIdAsync(long productId)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<List<Order>> GetOrdersByUserIdAsync(long userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetOrderItemCountAsync(long userId)
    {
        return await _context.OrderItems
            .Where(oi => oi.Order.UserId == userId)
            .CountAsync();
    }
}