using InframartAPI_New.DTOs;
using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InframartAPI_New.Controllers
{
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminVendorKycController : ControllerBase
    {
        private readonly IVendorKycService _vendorKycService;

        public AdminVendorKycController(IVendorKycService vendorKycService)
        {
            _vendorKycService = vendorKycService;
        }

        // GET /admin/vendor-kyc
        [HttpGet("admin/vendor-kyc")]
        public async Task<IActionResult> GetKycRequests([FromQuery] string? status)
        {
            var (success, error, data) = await _vendorKycService.GetKycRequestsAsync(status);
            if (!success)
            {
                return BadRequest(new { success = false, message = error });
            }
            return Ok(data);
        }

        // GET /admin/vendor-kyc/{vendorId}
        [HttpGet("admin/vendor-kyc/{vendorId}")]
        public async Task<IActionResult> GetKycDetails(long vendorId)
        {
            var (success, error, data) = await _vendorKycService.GetKycDetailsAsync(vendorId);
            if (!success)
            {
                return NotFound(new { success = false, message = error });
            }
            return Ok(data);
        }
    }
}
