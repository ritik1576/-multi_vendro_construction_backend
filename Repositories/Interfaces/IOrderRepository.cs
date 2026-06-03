using MultiVendorAPI.Models;

public interface IOrderRepository
{
    Task CreateOrderAsync(Order order);

    Task CreateOrderItemAsync(OrderItem orderItem);

    Task<Product?> GetProductByIdAsync(long productId);

    Task SaveChangesAsync();

    Task<List<Order>> GetOrdersByUserIdAsync(long userId);

    Task<int> GetOrderItemCountAsync(long userId);

}