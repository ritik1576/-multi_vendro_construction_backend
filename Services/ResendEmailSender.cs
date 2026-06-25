using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Resend;
using InframartAPI_New.Services.Interfaces;

namespace InframartAPI_New.Services
{
    public class ResendEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public ResendEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task<EmailResult> SendEmailAsync(string to, string subject, string htmlBody)
        {
            var result = new EmailResult();
            try
            {
                var apiKey = _config["ResendSettings:ApiKey"]!;
                var fromEmail = _config["ResendSettings:FromEmail"]!;
                var fromName = _config["ResendSettings:FromName"]!;

                var resend = ResendClient.Create(apiKey);

                var emailMessage = new EmailMessage
                {
                    From = $"{fromName} <{fromEmail}>",
                    To = to,
                    Subject = subject,
                    HtmlBody = htmlBody
                };

                // Send email through Resend SDK
                var response = await resend.EmailSendAsync(emailMessage);

                result.Success = true;
                result.MessageId = response?.ToString() ?? Guid.NewGuid().ToString();
                result.ProviderResponse = $"Sent successfully. Message ID: {result.MessageId}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.StackTrace = ex.StackTrace;
                result.ProviderResponse = ex.ToString();
            }

            return result;
        }
    }
}
