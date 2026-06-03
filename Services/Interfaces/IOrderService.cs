using MultiVendorAPI.Common;

public interface IOrderService
{
    Task<ServiceResponse<string>>
        CreateOrderAsync(CreateOrderDto dto);

    Task<ServiceResponse<List<OrderListDto>>>
    GetOrdersByUserIdAsync(long userId);
}