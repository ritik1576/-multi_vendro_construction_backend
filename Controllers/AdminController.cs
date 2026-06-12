using InframartAPI_New.DTOs;
using InframartAPI_New.DTOs.Auth;
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
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            var (success, error, data) = await _adminService.LoginAdminAsync(request);
            if (!success)
            {
                return Unauthorized(new { message = error });
            }

            return Ok(data);
        }

        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            return Ok("Admin Dashboard Access Granted");
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var (success, error, data) = await _adminService.GetAllUsersAsync();
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new
            {
                success = true,
                totalUsers = data!.Count,
                data
            });
        }

        [HttpGet("users/{id:long}")]
        public async Task<IActionResult> GetUserDetails(long id)
        {
            var (success, error, data) = await _adminService.GetUserDetailsAsync(id);
            if (!success)
                return NotFound(new { message = error });

            return Ok(new { success = true, data });
        }

        [HttpPut("users/suspend/{id:long}")]
        public async Task<IActionResult> SuspendUser(long id, [FromBody] UpdateUserStatusDto? dto)
        {
            string status = dto?.Status ?? "toggle";
            var (success, error) = await _adminService.UpdateUserStatusAsync(id, status);
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { success = true, message = $"User status updated successfully", userId = id });
        }

        [HttpGet("vendors/{id:long}")]
        public async Task<IActionResult> GetVendorDetails(long id)
        {
            var (success, error, data) = await _adminService.GetVendorDetailsAsync(id);
            if (!success)
                return NotFound(new { message = error });

            return Ok(new { success = true, data });
        }

        [HttpPut("vendors/{id:long}/approve")]
        public async Task<IActionResult> ApproveVendor(long id)
        {
            var (success, error) = await _adminService.UpdateVendorStatusAsync(id, "approved");
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { success = true, message = "Vendor approved successfully", vendorId = id });
        }

        [HttpPut("vendors/{id:long}/reject")]
        public async Task<IActionResult> RejectVendor(long id)
        {
            var (success, error) = await _adminService.UpdateVendorStatusAsync(id, "rejected");
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { success = true, message = "Vendor rejected successfully", vendorId = id });
        }

        [HttpPut("vendors/{id:long}/block")]
        public async Task<IActionResult> BlockVendor(long id)
        {
            var (success, error) = await _adminService.UpdateVendorStatusAsync(id, "suspended");
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { success = true, message = "Vendor blocked successfully", vendorId = id });
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            var (success, error, data) = await _adminService.GetAllOrdersAsync();
            if (!success)
                return BadRequest(new { message = error });

            return Ok(new
            {
                success = true,
                totalOrders = data!.Count,
                data
            });
        }

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