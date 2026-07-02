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

    Task<ServiceResponse<object>> CreateOnlinePaymentAsync(InframartAPI_New.DTOs.CreateOnlinePaymentDto request, long userId);
    Task<ServiceResponse<object>> VerifyOnlinePaymentAsync(InframartAPI_New.DTOs.VerifyOnlinePaymentDto request, long userId);
}
