using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using InframartAPI_New.Models;
using InframartAPI_New.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiVendorAPI.Data;

namespace InframartAPI_New.Services
{
    public class BrevoEmailService : IEmailService
    {
        private readonly IEmailTemplateService _templateService;
        private readonly ApplicationDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly ILogger<BrevoEmailService> _logger;
        private readonly BrevoSettings _settings;

        public BrevoEmailService(
            IEmailTemplateService templateService,
            ApplicationDbContext dbContext,
            HttpClient httpClient,
            IOptions<BrevoSettings> settings,
            ILogger<BrevoEmailService> logger)
        {
            _templateService = templateService;
            _dbContext = dbContext;
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<bool> SendAsync(string recipientEmail, string subject, string htmlBody)
        {
            string status = "Failed";
            string? errorMessage = null;
            string? providerMessageId = null;
            DateTime? sentAt = null;

            try
            {
                var payload = new BrevoEmailPayload
                {
                    Sender = new BrevoParty { Name = _settings.SenderName, Email = _settings.SenderEmail },
                    To = new List<BrevoParty> { new BrevoParty { Email = recipientEmail } },
                    Subject = subject,
                    HtmlContent = htmlBody
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending email to {Recipient} via Brevo HTTP API", recipientEmail);
                var response = await _httpClient.PostAsync("smtp/email", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    status = "Success";
                    sentAt = DateTime.UtcNow;

                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("messageId", out var msgIdProp))
                    {
                        providerMessageId = msgIdProp.GetString();
                    }

                    _logger.LogInformation("Email sent successfully to {Recipient}. Message ID: {MessageId}", recipientEmail, providerMessageId);
                    return true;
                }
                else
                {
                    errorMessage = $"Brevo API returned error. Status Code: {(int)response.StatusCode} ({response.StatusCode}). Response: {responseBody}";
                    _logger.LogError("Failed to send email to {Recipient}. Error: {Error}", recipientEmail, errorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Exception: {ex.Message}\nStack Trace: {ex.StackTrace}";
                _logger.LogError(ex, "Exception while sending email to {Recipient} via Brevo", recipientEmail);
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
                        RecipientEmail = recipientEmail,
                        Subject = subject,
                        Body = htmlBody,
                        Status = status,
                        ErrorMessage = errorMessage ?? $"MessageId: {providerMessageId}",
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

        public async Task<bool> SendTemplateAsync(string recipientEmail, string subject, string templatePath, Dictionary<string, string> variables)
        {
            try
            {
                // Render HTML template using the template service
                var (renderedSubject, renderedBody) = await _templateService.RenderTemplateAsync(templatePath, variables);
                var finalSubject = string.IsNullOrWhiteSpace(renderedSubject) ? subject : renderedSubject;
                return await SendAsync(recipientEmail, finalSubject, renderedBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template email send via Brevo. Template: {TemplatePath}, Recipient: {Recipient}", templatePath, recipientEmail);
                
                try
                {
                    var log = new EmailLog
                    {
                        TemplateId = null,
                        RecipientEmail = recipientEmail,
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

        // Inner classes matching Brevo payload JSON structure
        private class BrevoEmailPayload
        {
            [JsonPropertyName("sender")]
            public BrevoParty Sender { get; set; } = new();

            [JsonPropertyName("to")]
            public List<BrevoParty> To { get; set; } = new();

            [JsonPropertyName("subject")]
            public string Subject { get; set; } = string.Empty;

            [JsonPropertyName("htmlContent")]
            public string HtmlContent { get; set; } = string.Empty;
        }

        private class BrevoParty
        {
            [JsonPropertyName("name")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Name { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; } = string.Empty;
        }
    }
}
