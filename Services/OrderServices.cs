using MultiVendorAPI.Models;
using MultiVendorAPI.Common;
public class OrderServices : IOrderService
{
    private readonly IOrderRepository _orderRepository;

    public OrderServices(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<ServiceResponse<string>>
CreateOrderAsync(CreateOrderDto dto)
    {
        decimal subtotal = 0;

        var products = new List<Product>();

        foreach (var item in dto.Items)
        {
            var product =
                await _orderRepository.GetProductByIdAsync(item.ProductId);

            if (product == null)
            {
                return ServiceResponse<string>
                    .FailureResponse(
                        $"Product {item.ProductId} not found",
                        404);
            }

            subtotal += product.Price!.Value * item.Quantity;

            products.Add(product);
        }

        decimal shipping = 99;

        var order = new Order
        {

            AddressId = dto.AddressId,
            Subtotal = subtotal,
            DiscountAmount = 0,
            ShippingCharge = shipping,
            TotalAmount = subtotal + shipping,
            PaymentStatus = "pending",
            OrderStatus = "pending",
            PlacedAt = DateTime.UtcNow
        };

        await _orderRepository.CreateOrderAsync(order);

        await _orderRepository.SaveChangesAsync();

        for (int i = 0; i < dto.Items.Count; i++)
        {
            var item = dto.Items[i];

            var product = products[i];

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                ProductName = product.Name,
                Price = product.Price!.Value,
                CreatedAt = DateTime.UtcNow,
                TotalPrice = product.Price.Value * item.Quantity
            };

            await _orderRepository.CreateOrderItemAsync(orderItem);
        }

        await _orderRepository.SaveChangesAsync();

        return ServiceResponse<string>
            .SuccessResponse(
                "Order created successfully");
    }

    public async Task<ServiceResponse<List<OrderListDto>>>
    GetOrdersByUserIdAsync(long userId)
    {
        var orders =
            await _orderRepository.GetOrdersByUserIdAsync(userId);

        if (!orders.Any())
        {
            return ServiceResponse<List<OrderListDto>>
                .FailureResponse(
                    "No orders found",
                    404);
        }

        var result = new List<OrderListDto>();

        foreach (var order in orders)
        {
            var itemCount = await _orderRepository.GetOrderItemCountAsync(order.UserId);

            result.Add(new OrderListDto
            {
                Id = order.UserId,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                CreatedAt = order.PlacedAt,
                ItemCount = itemCount
            });
        }

        return ServiceResponse<List<OrderListDto>>
            .SuccessResponse(
                result,
                "Orders retrieved successfully");
    }
}
