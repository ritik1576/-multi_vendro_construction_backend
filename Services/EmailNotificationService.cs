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
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _dbContext;

        public EmailNotificationService(
            IEmailTemplateService templateService,
            IEmailSender emailSender,
            ApplicationDbContext dbContext)
        {
            _templateService = templateService;
            _emailSender = emailSender;
            _dbContext = dbContext;
        }

        public async Task<bool> SendTemplateEmailAsync(string templateKey, string email, Dictionary<string, string> variables)
        {
            EmailTemplateSetting? template = null;
            string subject = string.Empty;
            string body = string.Empty;
            string status = "Failed";
            string? errorMessage = null;
            string? providerResponse = null;
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

                // Send via Resend Email Sender
                var result = await _emailSender.SendEmailAsync(email, subject, body);
                
                providerResponse = result.ProviderResponse;
                if (result.Success)
                {
                    status = "Success"; // Requirement says Status = Success (or Sent, let's use Success as requested)
                    sentAt = DateTime.UtcNow;
                    return true;
                }
                else
                {
                    status = "Failed";
                    errorMessage = result.ErrorMessage;
                    // Prepend stack trace to error message or keep it structured
                    if (!string.IsNullOrEmpty(result.StackTrace))
                    {
                        errorMessage += $"\nStack Trace: {result.StackTrace}";
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                // If rendering succeeded but sending failed, we still want to log subject/body
                if (string.IsNullOrEmpty(subject) && template != null)
                {
                    subject = template.Subject;
                    body = "Failed to render body or template missing.";
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
                        ErrorMessage = string.IsNullOrEmpty(errorMessage) ? providerResponse : errorMessage,
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
