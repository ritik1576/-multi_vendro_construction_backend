using MultiVendorAPI.Common;

public interface IOrderService
{
    Task<ServiceResponse<PlaceOrderResponseDto>>
        CreateOrderAsync(CreateOrderDto dto);

    Task<ServiceResponse<List<OrderListDto>>>
        GetOrdersAsync(long? userId);

    Task<ServiceResponse<OrderDetailsDto>>
        GetOrderDetailsAsync(long orderId);

    Task<ServiceResponse<OrderDetailsDto>>
        CancelOrderAsync(long orderId);

    Task<ServiceResponse<OrderTrackingDto>>
        GetOrderTrackingAsync(long orderId);
}
