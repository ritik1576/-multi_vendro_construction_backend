using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace InframartAPI_New.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"]!,
                _config["EmailSettings:SenderEmail"]!
            ));

            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            email.Body = new TextPart("html")
            {
                Text = body
            };

            using var smtp = new SmtpClient();

            int port = int.Parse(_config["EmailSettings:Port"]!);
            var secureOption = port == 465 
                ? MailKit.Security.SecureSocketOptions.SslOnConnect 
                : MailKit.Security.SecureSocketOptions.StartTls;

            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"]!,
                port,
                secureOption
            );

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"]!,
                _config["EmailSettings:Password"]!
            );

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}