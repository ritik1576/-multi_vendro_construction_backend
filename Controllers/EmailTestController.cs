using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InframartAPI_New.Services.Interfaces;

namespace InframartAPI_New.Controllers
{
    public class TestEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    [Route("api/email")]
    [ApiController]
    [AllowAnonymous] // Allow testing without JWT auth
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailSender _emailSender;

        public EmailTestController(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestSend([FromBody] TestEmailRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, message = "Email is required" });
            }

            var subject = "InfraMart Test Email";
            var body = "<p>Email service is working successfully.</p>";

            var result = await _emailSender.SendEmailAsync(request.Email, subject, body);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = "Email sent successfully.",
                    providerResponse = result.ProviderResponse,
                    messageId = result.MessageId
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = result.ErrorMessage ?? "Failed to send email through Resend.",
                    error = result.ProviderResponse
                });
            }
        }
    }
}
