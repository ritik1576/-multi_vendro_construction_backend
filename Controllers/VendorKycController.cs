using InframartAPI_New.DTOs;
using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InframartAPI_New.Controllers
{
    [Route("vendor/kyc")]
    [ApiController]
    [Authorize(Roles = "vendor")]
    public class VendorKycController : ControllerBase
    {
        private readonly IVendorKycService _vendorKycService;

        public VendorKycController(IVendorKycService vendorKycService)
        {
            _vendorKycService = vendorKycService;
        }

        // POST /vendor/kyc
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitKyc([FromForm] KycSubmitDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { success = false, message = "Invalid KYC details." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "User is unauthorized." });
            }

            var (success, message) = await _vendorKycService.SubmitKycAsync(userId, dto);
            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message });
        }

        // GET /vendor/kyc/status
        [HttpGet("status")]
        public async Task<IActionResult> GetKycStatus()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "User is unauthorized." });
            }

            var (success, error, data) = await _vendorKycService.GetKycStatusAsync(userId);
            if (!success)
            {
                return BadRequest(new { success = false, message = error });
            }

            return Ok(data);
        }
    }
}
