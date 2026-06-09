using InframartAPI_New.DTOs.VendorDTOs;
using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InframartAPI_New.Controllers
{
    [Route("vendor")]
    [ApiController]
    [Authorize(Roles = "vendor")]
    public class VendorController : ControllerBase
    {
        private readonly IVendorOrderService _vendorOrderService;

        public VendorController(IVendorOrderService vendorOrderService)
        {
            _vendorOrderService = vendorOrderService;
        }

        // ─── Helper: extract vendorId claim from JWT ──────────────────────────
        private long? GetVendorIdFromToken()
        {
            var claim = User.FindFirst("vendorId")?.Value;
            return long.TryParse(claim, out var id) ? id : null;
        }

        // ─── Helper: extract userId claim from JWT ────────────────────────────
        private long? GetUserIdFromToken()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(claim, out var id) ? id : null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /vendor/status
        // Query param: ?userId=5  (or reads from JWT if omitted)
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus([FromQuery] long? userId)
        {
            var uid = userId ?? GetUserIdFromToken();
            if (uid == null)
                return BadRequest(new { message = "userId is required" });

            var (success, error, data) = await _vendorOrderService.GetVendorStatusAsync(uid.Value);
            if (!success)
                return NotFound(new { message = error });

            return Ok(new { success = true, data });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /vendor/orders
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var vendorId = GetVendorIdFromToken();
            if (vendorId == null)
                return Unauthorized(new { message = "Vendor ID not found in token" });

            var (success, error, data) = await _vendorOrderService.GetVendorOrdersAsync(vendorId.Value);
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
        // GET /vendor/orders/{orderId}
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("orders/{orderId:long}")]
        public async Task<IActionResult> GetOrderDetail(long orderId)
        {
            var vendorId = GetVendorIdFromToken();
            if (vendorId == null)
                return Unauthorized(new { message = "Vendor ID not found in token" });

            var (success, error, data) = await _vendorOrderService.GetVendorOrderDetailAsync(vendorId.Value, orderId);
            if (!success)
                return NotFound(new { message = error });

            return Ok(new { success = true, data });
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /vendor/orders/{orderId}/status
        // Body: { "orderStatus": "Shipped" }
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("orders/{orderId:long}/status")]
        public async Task<IActionResult> UpdateOrderStatus(long orderId, [FromBody] UpdateOrderStatusDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.OrderStatus))
                return BadRequest(new { message = "orderStatus is required" });

            var vendorId = GetVendorIdFromToken();
            if (vendorId == null)
                return Unauthorized(new { message = "Vendor ID not found in token" });

            var (success, error) = await _vendorOrderService.UpdateOrderStatusAsync(vendorId.Value, orderId, dto);
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { success = true, message = "Order status updated successfully", orderId, newStatus = dto.OrderStatus });
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE /vendor/orders/{orderId}
        // ─────────────────────────────────────────────────────────────────────
        [HttpDelete("orders/{orderId:long}")]
        public async Task<IActionResult> DeleteOrder(long orderId)
        {
            var vendorId = GetVendorIdFromToken();
            if (vendorId == null)
                return Unauthorized(new { message = "Vendor ID not found in token" });

            var (success, error) = await _vendorOrderService.DeleteOrderAsync(vendorId.Value, orderId);
            if (!success)
                return NotFound(new { message = error });

            return Ok(new { success = true, message = "Order deleted successfully", orderId });
        }
    }
}