using InframartAPI_New.DTOs.VendorDTOs;
using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InframartAPI_New.Controllers
{
    [Route("vendor")]
    [ApiController]
    // [Authorize(Roles = "vendor")] // Temporarily disabled for simple testing
    public class VendorController : ControllerBase
    {
        private readonly IVendorOrderService _vendorOrderService;
        private readonly IVendorService _vendorService;

        public VendorController(IVendorOrderService vendorOrderService, IVendorService vendorService)
        {
            _vendorOrderService = vendorOrderService;
            _vendorService = vendorService;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /vendor/products/{vendorId}
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("products/{vendorId:long}")]
        public async Task<IActionResult> GetProducts(long vendorId)
        {
            var (success, error, data) = await _vendorOrderService.GetVendorProductsAsync(vendorId);
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new
            {
                success = true,
                totalProducts = data!.Count,
                data
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /vendor/{vendorId}/status
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("status/{vendorId:long}")]
        public async Task<IActionResult> GetStatus(long vendorId)
        {
            // Note: We are using a new method GetVendorStatusByVendorIdAsync or we can reuse the existing one if we pass userId. 
            // For simplicity, we will assume GetVendorStatusByVendorIdAsync is added to the service.
            // But since GetVendorStatusAsync takes userId currently, we need to modify the service too.
            // Let's call a new method that we will add.
            var (success, error, data) = await _vendorOrderService.GetVendorStatusByVendorIdAsync(vendorId);
            if (!success)
                return NotFound(new { message = error });

            return Ok(new { success = true, data });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /vendor/{vendorId}/orders
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("orders/{vendorId:long}")]
        public async Task<IActionResult> GetOrders(long vendorId)
        {
            var (success, error, data) = await _vendorOrderService.GetVendorOrdersAsync(vendorId);
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new
            {
                success = true,
                totalOrders = data!.Count,
                data
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /vendor/{vendorId}/orders/{orderId}
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{vendorId:long}/orders/{orderId:long}")]
        public async Task<IActionResult> GetOrderDetail(long vendorId, long orderId)
        {
            var (success, error, data) = await _vendorOrderService.GetVendorOrderDetailAsync(vendorId, orderId);
            if (!success)
                return NotFound(new { message = error });

            return Ok(new { success = true, data });
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /vendor/{vendorId}/orders/{orderId}/status
        // Body: { "orderStatus": "Shipped" }
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("{vendorId:long}/orders/{orderId:long}/status")]
        public async Task<IActionResult> UpdateOrderStatus(long vendorId, long orderId, [FromBody] UpdateOrderStatusDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.OrderStatus))
                return BadRequest(new { message = "orderStatus is required" });

            var (success, error) = await _vendorOrderService.UpdateOrderStatusAsync(vendorId, orderId, dto);
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { success = true, message = "Order status updated successfully", orderId, newStatus = dto.OrderStatus });
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE /vendor/{vendorId}/orders/{orderId}
        // ─────────────────────────────────────────────────────────────────────
        [HttpDelete("{vendorId:long}/orders/{orderId:long}")]
        public async Task<IActionResult> DeleteOrder(long vendorId, long orderId)
        {
            var (success, error) = await _vendorOrderService.DeleteOrderAsync(vendorId, orderId);
            if (!success)
                return NotFound(new { message = error });

            return Ok(new { success = true, message = "Order deleted successfully", orderId });
        }
    }
}