using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using InframartAPI_New.DTOs;
using InframartAPI_New.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Data;

namespace InframartAPI_New.Controllers
{
    /// <summary>
    /// Admin APIs for managing Email Templates.
    /// </summary>
    [Route("admin/email-templates")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class EmailTemplatesController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public EmailTemplatesController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Create a new email template.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTemplate([FromBody] EmailTemplateCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var keyExists = await _dbContext.EmailTemplates
                .AnyAsync(t => t.TemplateKey == dto.TemplateKey);

            if (keyExists)
            {
                return BadRequest(new { message = $"Template key '{dto.TemplateKey}' already exists. It must be unique." });
            }

            var adminEmail = User.FindFirst(ClaimTypes.Name)?.Value ?? "admin";

            var template = new EmailTemplate
            {
                TemplateKey = dto.TemplateKey.Trim().ToUpper(),
                TemplateName = dto.TemplateName.Trim(),
                Subject = dto.Subject.Trim(),
                HtmlContent = dto.HtmlContent,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminEmail
            };

            _dbContext.EmailTemplates.Add(template);
            await _dbContext.SaveChangesAsync();

            var response = MapToResponseDto(template);
            return CreatedAtAction(nameof(GetTemplateById), new { id = template.Id }, response);
        }

        /// <summary>
        /// Get all email templates.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllTemplates()
        {
            var templates = await _dbContext.EmailTemplates
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var responseList = templates.Select(MapToResponseDto).ToList();
            return Ok(responseList);
        }

        /// <summary>
        /// Get email template by ID.
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetTemplateById(long id)
        {
            var template = await _dbContext.EmailTemplates.FindAsync(id);
            if (template == null)
            {
                return NotFound(new { message = $"Email template with ID {id} not found." });
            }

            return Ok(MapToResponseDto(template));
        }

        /// <summary>
        /// Update an existing email template.
        /// </summary>
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateTemplate(long id, [FromBody] EmailTemplateUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var template = await _dbContext.EmailTemplates.FindAsync(id);
            if (template == null)
            {
                return NotFound(new { message = $"Email template with ID {id} not found." });
            }

            var adminEmail = User.FindFirst(ClaimTypes.Name)?.Value ?? "admin";

            template.TemplateName = dto.TemplateName.Trim();
            template.Subject = dto.Subject.Trim();
            template.HtmlContent = dto.HtmlContent;
            template.IsActive = dto.IsActive;
            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = adminEmail;

            _dbContext.EmailTemplates.Update(template);
            await _dbContext.SaveChangesAsync();

            return Ok(MapToResponseDto(template));
        }

        /// <summary>
        /// Delete an email template.
        /// </summary>
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteTemplate(long id)
        {
            var template = await _dbContext.EmailTemplates.FindAsync(id);
            if (template == null)
            {
                return NotFound(new { message = $"Email template with ID {id} not found." });
            }

            _dbContext.EmailTemplates.Remove(template);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, message = "Email template deleted successfully." });
        }

        /// <summary>
        /// Toggle the active status of an email template.
        /// </summary>
        [HttpPut("{id:long}/status")]
        public async Task<IActionResult> ToggleStatus(long id, [FromBody] EmailTemplateStatusDto? dto = null)
        {
            var template = await _dbContext.EmailTemplates.FindAsync(id);
            if (template == null)
            {
                return NotFound(new { message = $"Email template with ID {id} not found." });
            }

            var adminEmail = User.FindFirst(ClaimTypes.Name)?.Value ?? "admin";

            if (dto != null)
            {
                template.IsActive = dto.IsActive;
            }
            else
            {
                template.IsActive = !template.IsActive;
            }

            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = adminEmail;

            _dbContext.EmailTemplates.Update(template);
            await _dbContext.SaveChangesAsync();

            return Ok(new 
            { 
                success = true, 
                message = $"Email template status updated successfully.", 
                id = template.Id, 
                isActive = template.IsActive 
            });
        }

        private static EmailTemplateResponseDto MapToResponseDto(EmailTemplate template)
        {
            return new EmailTemplateResponseDto
            {
                Id = template.Id,
                TemplateKey = template.TemplateKey,
                TemplateName = template.TemplateName,
                Subject = template.Subject,
                HtmlContent = template.HtmlContent,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                CreatedBy = template.CreatedBy,
                UpdatedBy = template.UpdatedBy
            };
        }
    }
}
