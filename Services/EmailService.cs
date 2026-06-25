using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InframartAPI_New.Models;
using InframartAPI_New.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MultiVendorAPI.Data;

namespace InframartAPI_New.Services
{
    public class EmailService : IEmailService
    {
        private readonly IEmailTemplateService _templateService;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<EmailService> _logger;
        
        private readonly string _smtpServer;
        private readonly int _port;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _password;

        public EmailService(
            IEmailTemplateService templateService,
            ApplicationDbContext dbContext,
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _templateService = templateService;
            _dbContext = dbContext;
            _logger = logger;

            // Load settings from appsettings.json
            _smtpServer = configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            _port = int.TryParse(configuration["EmailSettings:Port"], out var p) ? p : 587;
            _senderEmail = configuration["EmailSettings:SenderEmail"] ?? "inframart102@gmail.com";
            _senderName = configuration["EmailSettings:SenderName"] ?? "InfraMart Support";
            _password = configuration["EmailSettings:Password"] ?? "mtwb mpda yrpn mzuh";
        }

        public async Task<bool> SendAsync(string to, string subject, string htmlBody)
        {
            string status = "Failed";
            string? errorMessage = null;
            DateTime? sentAt = null;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_senderName, _senderEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // For Gmail port 587, use StartTls
                    await client.ConnectAsync(_smtpServer, _port, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_senderEmail, _password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                status = "Success"; // Or "Sent" - using "Success" to align with current logging database comments/convention
                sentAt = DateTime.UtcNow;
                _logger.LogInformation("Email successfully sent to {Recipient} with subject: {Subject}", to, subject);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger.LogError(ex, "Failed to send email to {Recipient} with subject: {Subject}", to, subject);
                return false;
            }
            finally
            {
                // Write log to DB
                try
                {
                    var log = new EmailLog
                    {
                        TemplateId = null,
                        RecipientEmail = to,
                        Subject = subject,
                        Body = htmlBody,
                        Status = status,
                        ErrorMessage = errorMessage,
                        SentAt = sentAt,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.EmailLogs.Add(log);
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to save email log to the database.");
                }
            }
        }

        public async Task<bool> SendTemplateAsync(string to, string subject, string templatePath, Dictionary<string, string> variables)
        {
            try
            {
                // Render HTML template using the template service
                string renderedBody = await _templateService.GetRenderedTemplateAsync(templatePath, variables);
                return await SendAsync(to, subject, renderedBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template email send. Template: {TemplatePath}, Recipient: {Recipient}", templatePath, to);
                
                // Write failure log to DB if we can
                try
                {
                    var log = new EmailLog
                    {
                        TemplateId = null,
                        RecipientEmail = to,
                        Subject = subject,
                        Body = $"Failed to render template {templatePath}",
                        Status = "Failed",
                        ErrorMessage = ex.ToString(),
                        SentAt = null,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.EmailLogs.Add(log);
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to save failure email log.");
                }

                return false;
            }
        }
    }
}
