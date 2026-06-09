using InframartAPI_New.DTOs.VendorDTOs;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IVendorOrderService
    {
        /// <summary>GET /vendor/status?userId={id}</summary>
        Task<(bool success, string? error, VendorStatusDto? data)> GetVendorStatusAsync(long userId);

        /// <summary>GET /vendor/orders — all orders containing this vendor's products</summary>
        Task<(bool success, string? error, List<VendorOrderListDto>? data)> GetVendorOrdersAsync(long vendorId);

        /// <summary>GET /vendor/orders/{orderId}</summary>
        Task<(bool success, string? error, VendorOrderDetailDto? data)> GetVendorOrderDetailAsync(long vendorId, long orderId);

        /// <summary>PUT /vendor/orders/{orderId}/status</summary>
        Task<(bool success, string? error)> UpdateOrderStatusAsync(long vendorId, long orderId, UpdateOrderStatusDto dto);

        /// <summary>DELETE /vendor/orders/{orderId}</summary>
        Task<(bool success, string? error)> DeleteOrderAsync(long vendorId, long orderId);
    }
}
