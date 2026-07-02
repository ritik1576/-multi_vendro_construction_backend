using MultiVendorAPI.Models;

public interface IOrderRepository
{
    Task CreateOrderAsync(Order order);

    Task CreateOrderItemAsync(OrderItem orderItem);

    Task<Product?> GetProductByIdAsync(long productId);

    Task SaveChangesAsync();

    Task<List<Order>> GetOrdersAsync(long? userId);

    Task<List<Order>> GetOrdersByUserIdAsync(long userId);

    Task<Order?> GetOrderByIdWithItemsAsync(long orderId);

    Task<int> GetOrderItemCountAsync(long orderId);

}
