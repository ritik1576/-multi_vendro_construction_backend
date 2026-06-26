using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InframartAPI_New.Models;
using InframartAPI_New.Services;
using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Data;

namespace InframartAPI_New.Controllers
{
    [Route("admin/email-templates")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class EmailTemplatesController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailTemplateService _templateService;

        private static readonly HashSet<string> RestrictedRuntimeVariables = new(StringComparer.OrdinalIgnoreCase)
        {
            "CustomerName", "VendorName", "OrderNumber", "OrderDate", "OTP", "ResetLink",
            "TransactionId", "Amount", "WalletBalance", "RefundAmount", "DeliveryAddress",
            "PaymentMethod", "DashboardUrl", "TrackOrderUrl", "InvoiceUrl", "ReviewProductUrl"
        };

        public EmailTemplatesController(ApplicationDbContext dbContext, IEmailTemplateService templateService)
        {
            _dbContext = dbContext;
            _templateService = templateService;
        }

        // GET /admin/email-templates
        [HttpGet]
        public async Task<IActionResult> GetAllTemplates()
        {
            // Seed any template settings from metadata that are not in database yet
            foreach (var key in EmailTemplateService.TemplateMetadata.Keys)
            {
                await _templateService.GetTemplateAsync(key);
            }

            var settings = await _dbContext.EmailTemplateSettings
                .OrderBy(s => s.TemplateKey)
                .ToListAsync();

            var result = settings.Select(s => new
            {
                s.TemplateKey,
                s.Subject,
                s.IsActive,
                s.CreatedAt,
                s.UpdatedAt
            });

            return Ok(result);
        }

        // GET /admin/email-templates/{templateKey}
        [HttpGet("{templateKey}")]
        public async Task<IActionResult> GetTemplateDetails(string templateKey)
        {
            var setting = await _templateService.GetTemplateAsync(templateKey);
            if (setting == null)
            {
                return NotFound(new { message = $"Email template with key '{templateKey}' not found." });
            }

            var variables = setting.Variables.ToDictionary(v => v.VariableKey, v => v.VariableValue);

            return Ok(new
            {
                setting.TemplateKey,
                setting.Subject,
                setting.IsActive,
                EditableVariables = variables
            });
        }

        // PUT /admin/email-templates/{templateKey}
        [HttpPut("{templateKey}")]
        public async Task<IActionResult> UpdateTemplate(string templateKey, [FromBody] UpdateTemplateRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid request payload." });
            }

            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                return BadRequest(new { message = "Subject cannot be empty." });
            }

            var setting = await _templateService.GetTemplateAsync(templateKey);
            if (setting == null)
            {
                return NotFound(new { message = $"Email template with key '{templateKey}' not found." });
            }

            // Get metadata defaults to validate variables
            if (!EmailTemplateService.TemplateMetadata.TryGetValue(templateKey, out var meta))
            {
                return BadRequest(new { message = "Predefined metadata not found for this template key." });
            }

            // Validate duplicate variable names in request
            if (request.EditableVariables.Keys.GroupBy(k => k, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            {
                return BadRequest(new { message = "Duplicate variable names are not allowed." });
            }

            // Validation checks
            foreach (var kvp in request.EditableVariables)
            {
                var key = kvp.Key;

                // 1. Reject if it is a runtime variable
                if (RestrictedRuntimeVariables.Contains(key))
                {
                    return BadRequest(new { message = $"Variable '{key}' is a runtime variable and cannot be edited." });
                }

                // 2. Reject unknown variable names
                if (!meta.DefaultVars.ContainsKey(key))
                {
                    return BadRequest(new { message = $"Variable '{key}' is not allowed or unknown for this template." });
                }
            }

            // Update fields
            setting.Subject = request.Subject.Trim();
            setting.IsActive = request.IsActive;
            setting.UpdatedAt = DateTime.UtcNow;

            // Clear old variables and recreate them
            _dbContext.EmailTemplateVariables.RemoveRange(setting.Variables);

            setting.Variables = request.EditableVariables.Select(v => new EmailTemplateVariable
            {
                TemplateSettingId = setting.Id,
                VariableKey = v.Key,
                VariableValue = v.Value ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                setting.TemplateKey,
                setting.Subject,
                setting.IsActive,
                EditableVariables = request.EditableVariables
            });
        }

        // POST /admin/email-templates/reset/{templateKey}
        [HttpPost("reset/{templateKey}")]
        public async Task<IActionResult> ResetTemplate(string templateKey)
        {
            var setting = await _dbContext.EmailTemplateSettings
                .Include(s => s.Variables)
                .FirstOrDefaultAsync(s => s.TemplateKey == templateKey.ToUpper());

            if (setting == null)
            {
                return NotFound(new { message = $"Email template setting for key '{templateKey}' not found." });
            }

            if (!EmailTemplateService.TemplateMetadata.TryGetValue(templateKey, out var meta))
            {
                return BadRequest(new { message = "Predefined default values not found for this template key." });
            }

            setting.Subject = meta.DefaultSubject;
            setting.UpdatedAt = DateTime.UtcNow;

            _dbContext.EmailTemplateVariables.RemoveRange(setting.Variables);

            setting.Variables = meta.DefaultVars.Select(v => new EmailTemplateVariable
            {
                TemplateSettingId = setting.Id,
                VariableKey = v.Key,
                VariableValue = v.Value,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Template successfully reset to default values.",
                setting.TemplateKey,
                setting.Subject,
                setting.IsActive,
                EditableVariables = meta.DefaultVars
            });
        }

        // GET /admin/email-templates/test/{templateKey}
        [HttpGet("test/{templateKey}")]
        [AllowAnonymous] // Allow quick previewing in browser
        public async Task<IActionResult> TestRenderTemplate(string templateKey, [FromQuery] bool json = false)
        {
            var key = templateKey.ToUpper();
            var dummyVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "CustomerName", "John Doe" },
                { "VendorName", "Acme Construction Supply" },
                { "OTP", "123456" },
                { "ExpiryMinutes", "10" },
                { "ResetLink", "https://inframart.com/reset-password?token=mock_token_123" },
                { "LoginUrl", "https://inframart.com/login" },
                { "DashboardUrl", "https://inframart.com/dashboard" },
                { "OrderNumber", "INFR-8942-0192" },
                { "OrderDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
                { "TotalAmount", "12500.00" },
                { "ShippingCharge", "150.00" },
                { "Discount", "500.00" },
                { "FinalAmount", "12150.00" },
                { "TrackOrderUrl", "https://inframart.com/orders/track/123" },
                { "DeliveredDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
                { "ReviewProductUrl", "https://inframart.com/orders/review/123" },
                { "TransactionId", "TXN_MOCK_984392" },
                { "PaymentMethod", "Razorpay" },
                { "PaidAmount", "12150.00" },
                { "InvoiceUrl", "https://inframart.com/invoices/download/123" },
                { "FailureReason", "Insufficient funds in customer card." },
                { "RetryPaymentUrl", "https://inframart.com/checkout/retry/123" },
                { "RefundAmount", "12150.00" },
                { "RefundDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
                { "Amount", "5000.00" },
                { "WalletBalance", "7500.00" },
                { "WalletUrl", "https://inframart.com/wallet" },
                { "RejectReason", "GST Certificate scan is blur and unreadable." },
                { "ResubmitUrl", "https://inframart.com/vendor/kyc/resubmit" },
                { "ApprovalDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
            };

            try
            {
                var (subject, body) = await _templateService.RenderTemplateAsync(key, dummyVars, "test_recipient@gmail.com");
                
                if (json)
                {
                    return Ok(new { subject, body });
                }

                return Content(body, "text/html");
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = $"Failed to render template: {ex.Message}",
                    details = ex.ToString()
                });
            }
        }
    }

    public class UpdateTemplateRequest
    {
        public string Subject { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public Dictionary<string, string> EditableVariables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
