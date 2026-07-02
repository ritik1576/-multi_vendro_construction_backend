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

    public class VendorKycController : ControllerBase
    {
        private readonly IVendorKycService _vendorKycService;

        public VendorKycController(IVendorKycService vendorKycService)
        {
            _vendorKycService = vendorKycService;
        }

        // POST /vendor/kyc
        [HttpPost]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitKyc([FromForm] KycSubmitDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { success = false, message = "Invalid KYC details." });
            }

            var (success, message) = await _vendorKycService.SubmitKycAsync(dto);
            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message });
        }

        // GET /vendor/kyc/status
        [HttpGet("status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetKycStatus([FromQuery] long vendorId)
        {
            var (success, error, data) = await _vendorKycService.GetKycStatusAsync(vendorId);
            if (!success)
            {
                return BadRequest(new { success = false, message = error });
            }

            return Ok(data);
        }
    }
}
