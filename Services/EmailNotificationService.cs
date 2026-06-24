using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InframartAPI_New.Models;
using InframartAPI_New.Services.Interfaces;
using MultiVendorAPI.Data;

namespace InframartAPI_New.Services
{
    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly IEmailTemplateService _templateService;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _dbContext;

        public EmailNotificationService(
            IEmailTemplateService templateService,
            IEmailService emailService,
            ApplicationDbContext dbContext)
        {
            _templateService = templateService;
            _emailService = emailService;
            _dbContext = dbContext;
        }

        public async Task<bool> SendTemplateEmailAsync(string templateKey, string email, Dictionary<string, string> variables)
        {
            EmailTemplate? template = null;
            string subject = string.Empty;
            string body = string.Empty;
            string status = "Failed";
            string? errorMessage = null;
            DateTime? sentAt = null;

            try
            {
                template = await _templateService.GetTemplateAsync(templateKey);
                
                if (template == null)
                {
                    errorMessage = $"Template with key '{templateKey}' not found.";
                    return false;
                }

                if (!template.IsActive)
                {
                    errorMessage = $"Template with key '{templateKey}' is inactive.";
                    return false;
                }

                var (renderedSubject, renderedBody) = await _templateService.RenderTemplateAsync(templateKey, variables);
                subject = renderedSubject;
                body = renderedBody;

                await _emailService.SendEmailAsync(email, subject, body);
                
                status = "Sent";
                sentAt = DateTime.UtcNow;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                // If rendering succeeded but sending failed, we still want to log subject/body
                if (string.IsNullOrEmpty(subject) && template != null)
                {
                    subject = template.Subject;
                    body = template.HtmlContent;
                }
                return false;
            }
            finally
            {
                try
                {
                    var log = new EmailLog
                    {
                        TemplateId = template?.Id,
                        RecipientEmail = email,
                        Subject = string.IsNullOrEmpty(subject) ? $"[{templateKey}] Pending/Failed Email" : subject,
                        Body = string.IsNullOrEmpty(body) ? "Failed to render body or template missing." : body,
                        Status = status,
                        ErrorMessage = errorMessage,
                        SentAt = sentAt,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.EmailLogs.Add(log);
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Critical: Failed to save email log. Error: {logEx.Message}");
                }
            }
        }
    }
}
