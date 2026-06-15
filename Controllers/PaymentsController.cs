using System;
using System.Security.Claims;
using System.Threading.Tasks;
using InframartAPI_New.DTOs;
using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InframartAPI_New.Controllers
{
    [ApiController]
    [Route("payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !long.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("User identification token is invalid or missing.");
            return userId;
        }

        // ================= CREATE PAYMENT =================
        [HttpPost]
        [Authorize(Roles = "customer")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto request)
        {
            var userId = GetCurrentUserId();
            var result = await _paymentService.CreatePaymentAsync(request, userId);
            if (!result.Success) 
                return StatusCode(result.StatusCode, new { message = result.Message });
            return StatusCode(result.StatusCode, result.Data);
        }

        // ================= VERIFY PAYMENT =================
        [HttpPost("verify")]
        [Authorize(Roles = "customer")]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentDto request)
        {
            var userId = GetCurrentUserId();
            var result = await _paymentService.VerifyPaymentAsync(request, userId);
            if (!result.Success) 
                return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(result.Data);
        }

        // ================= PAYMENT HISTORY =================
        [HttpGet("history")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> PaymentHistory()
        {
            var result = await _paymentService.PaymentHistoryAsync();
            if (!result.Success) 
                return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(result.Data);
        }

        // ================= REFUND PAYMENT =================
        [HttpPost("refund")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RefundPayment([FromBody] RefundDto request)
        {
            var result = await _paymentService.RefundPaymentAsync(request);
            if (!result.Success) 
                return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(result.Data);
        }
    }
}
