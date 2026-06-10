using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InframartAPI_New.Controllers
{
    public class UpdateVendorStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    [Route("admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // [Authorize(Roles = "admin")]
        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            return Ok("Admin Dashboard Access Granted");
        }

        // [Authorize(Roles = "admin")]
        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            return Ok("Only admin can see all users");
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /admin/vendors
        // ─────────────────────────────────────────────────────────────────────
        // [Authorize(Roles = "admin")]
        [HttpGet("vendors")]
        public async Task<IActionResult> GetAllVendors()
        {
            var (success, error, data) = await _adminService.GetAllVendorsAsync();
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new
            {
                success = true,
                totalVendors = data!.Count,
                data
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /admin/vendors/{vendorId}/status
        // Body: { "status": "active" }
        // ─────────────────────────────────────────────────────────────────────
        // [Authorize(Roles = "admin")]
        [HttpPut("vendors/{vendorId:long}/status")]
        public async Task<IActionResult> UpdateVendorStatus(long vendorId, [FromBody] UpdateVendorStatusDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { message = "status is required" });

            var (success, error) = await _adminService.UpdateVendorStatusAsync(vendorId, dto.Status);
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { success = true, message = "Vendor status updated successfully", vendorId, newStatus = dto.Status });
        }
    }
}