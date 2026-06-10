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
        // Body: { "status": "shipped" }
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("{vendorId:long}/orders/{orderId:long}/status")]
        public async Task<IActionResult> UpdateOrderStatus(long vendorId, long orderId, [FromBody] UpdateOrderStatusDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { message = "status is required" });

            var (success, error) = await _vendorOrderService.UpdateOrderStatusAsync(vendorId, orderId, dto);
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { success = true, message = "Order status updated successfully", orderId, newStatus = dto.Status });
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /vendor/orders/{orderId}/status
        // Body: { "status": "shipped" }
        // Note: Assumes JWT auth will provide vendor ID in a real scenario, but for now we require vendorId in query string or we just update it.
        // Let's add a simplified route that bypasses vendorId check for simplicity if requested explicitly.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("orders/{orderId:long}/status")]
        public async Task<IActionResult> UpdateOrderStatusSlim(long orderId, [FromQuery] long vendorId, [FromBody] UpdateOrderStatusDto dto)
        {
            if (vendorId <= 0) return BadRequest(new { message = "vendorId query parameter is required" });
            return await UpdateOrderStatus(vendorId, orderId, dto);
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

        // ─────────────────────────────────────────────────────────────────────
        // GET /vendor/{userId}/orders
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{userId:long}/orders")]
        public async Task<IActionResult> GetOrdersByUserId(long userId)
        {
            var (success, error, data) = await _vendorOrderService.GetVendorOrdersByUserIdAsync(userId);
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
        // GET /vendor/{userId}/dashboard
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{userId:long}/dashboard")]
        public async Task<IActionResult> GetVendorDashboard(long userId)
        {
            var (statusSuccess, statusError, statusData) = await _vendorOrderService.GetVendorStatusAsync(userId);
            if (!statusSuccess || statusData == null)
                return BadRequest(new { message = "Vendor not found for this user" });

            var (success, error, data) = await _vendorOrderService.GetVendorDashboardAsync(statusData.VendorId);
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { success = true, data });
        }
    }
}