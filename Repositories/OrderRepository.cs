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

    public async Task<List<Order>> GetOrdersAsync(long? userId)
    {
        var query = _context.Orders.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(o => o.UserId == userId.Value);
        }

        return await query
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.PlacedAt)
            .ThenByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByIdWithItemsAsync(long orderId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<int> GetOrderItemCountAsync(long orderId)
    {
        return await _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .CountAsync();
    }
}
