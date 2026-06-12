using InframartAPI_New.DTOs.VendorDTOs;
using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InframartAPI_New.Middlewares;

namespace InframartAPI_New.Controllers
{
    [Route("vendor")]
    [ApiController]
    [Authorize(Roles = "vendor")]
    public class VendorController : ControllerBase
    {
        private readonly IVendorOrderService _vendorOrderService;
        private readonly IVendorService _vendorService;

        public VendorController(IVendorOrderService vendorOrderService, IVendorService vendorService)
        {
            _vendorOrderService = vendorOrderService;
            _vendorService = vendorService;
        }

        private void ValidateVendorAccess(long vendorId)
        {
            var vendorIdClaim = User.FindFirst("vendorId")?.Value;
            if (string.IsNullOrEmpty(vendorIdClaim) || !long.TryParse(vendorIdClaim, out var claimVendorId) || claimVendorId != vendorId)
            {
                throw new ForbiddenException("Access denied to this vendor's resources.");
            }
        }

        private void ValidateUserAccess(long userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var claimUserId) || claimUserId != userId)
            {
                throw new ForbiddenException("Access denied to this user's resources.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /vendor/products/{vendorId}
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("products/{vendorId:long}")]
        public async Task<IActionResult> GetProducts(long vendorId)
        {
            ValidateVendorAccess(vendorId);

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
            ValidateVendorAccess(vendorId);

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
            ValidateVendorAccess(vendorId);

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
            ValidateVendorAccess(vendorId);

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
            ValidateVendorAccess(vendorId);

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
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("orders/{orderId:long}/status")]
        public async Task<IActionResult> UpdateOrderStatusSlim(long orderId, [FromQuery] long vendorId, [FromBody] UpdateOrderStatusDto dto)
        {
            ValidateVendorAccess(vendorId);
            return await UpdateOrderStatus(vendorId, orderId, dto);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE /vendor/{vendorId}/orders/{orderId}
        // ─────────────────────────────────────────────────────────────────────
        [HttpDelete("{vendorId:long}/orders/{orderId:long}")]
        public async Task<IActionResult> DeleteOrder(long vendorId, long orderId)
        {
            ValidateVendorAccess(vendorId);

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
            ValidateUserAccess(userId);

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
            ValidateUserAccess(userId);

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