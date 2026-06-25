using System;
using System.Collections.Generic;
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

    public class TestTemplateEmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public string TemplatePath { get; set; } = string.Empty;
        public Dictionary<string, string> Variables { get; set; } = new();
    }

    [Route("api/email")]
    [ApiController]
    [AllowAnonymous] // Allow testing without JWT auth
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestSend([FromBody] TestEmailRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, message = "Email is required" });
            }

            var subject = "InfraMart Gmail SMTP Test Email";
            var body = "<h3>InfraMart Test</h3><p>Your Gmail SMTP mail delivery is working perfectly!</p>";

            var success = await _emailService.SendAsync(request.Email, subject, body);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = "Test email sent successfully via Gmail SMTP."
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to send email. Check logs for SMTP or connection errors."
                });
            }
        }

        [HttpPost("test-template")]
        public async Task<IActionResult> TestTemplateSend([FromBody] TestTemplateEmailRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.TemplatePath))
            {
                return BadRequest(new { success = false, message = "Email and TemplatePath are required" });
            }

            var subject = $"InfraMart Template Test - {request.TemplatePath}";
            
            var success = await _emailService.SendTemplateAsync(
                request.Email, 
                subject, 
                request.TemplatePath, 
                request.Variables
            );

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = $"Template email sent successfully using: {request.TemplatePath}"
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to send template email. Verify that the template file exists on disk and SMTP is working."
                });
            }
        }
    }
}
