using MultiVendorAPI.Common;

public interface IOrderService
{
    Task<ServiceResponse<PlaceOrderResponseDto>>
        CreateOrderAsync(CreateOrderDto dto);

    Task<ServiceResponse<List<OrderListDto>>>
        GetOrdersAsync(long? userId);

    Task<ServiceResponse<OrderDetailsDto>>
        GetOrderDetailsAsync(long orderId, long currentUserId, string userRole);

    Task<ServiceResponse<OrderDetailsDto>>
        CancelOrderAsync(long orderId, long currentUserId, string userRole);

    Task<ServiceResponse<OrderTrackingDto>>
        GetOrderTrackingAsync(long orderId, long currentUserId, string userRole);

    Task<ServiceResponse<List<OrderWithItemsDto>>> GetAllOrdersWithItemsAsync();
}
