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
            string host = _config["EmailSettings:SmtpServer"]!;
            int port = int.Parse(_config["EmailSettings:Port"]!);
            string senderEmail = _config["EmailSettings:SenderEmail"]!;
            string senderName = _config["EmailSettings:SenderName"]!;

            Console.WriteLine($"[SMTP DIAGNOSTICS] Configuration Loaded: Host={host}, Port={port}, Sender={senderEmail}, Name={senderName}");

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            smtp.Timeout = 10000; // 10 seconds connection and operation timeout
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true; // Bypass certificate validation interception

            var secureOption = port == 465 
                ? MailKit.Security.SecureSocketOptions.SslOnConnect 
                : MailKit.Security.SecureSocketOptions.StartTls;

            Console.WriteLine($"[SMTP DIAGNOSTICS] SmtpClient Initialized. Timeout={smtp.Timeout}ms, SecureOption={secureOption}");

            // Resolve host to IPv4 to prevent IPv6 routing hangs
            Console.WriteLine($"[SMTP DIAGNOSTICS] Resolving Host: {host}");
            try
            {
                var addresses = await System.Net.Dns.GetHostAddressesAsync(host);
                var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (ipv4 != null)
                {
                    Console.WriteLine($"[SMTP DIAGNOSTICS] DNS Resolved: {host} -> IPv4 {ipv4}");
                    host = ipv4.ToString();
                }
                else
                {
                    Console.WriteLine($"[SMTP DIAGNOSTICS] DNS Resolved: {host} (No IPv4 address found)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP DIAGNOSTICS] DNS resolution failed for {host}, falling back to hostname. Error: {ex.Message}");
            }

            try
            {
                Console.WriteLine($"[SMTP DIAGNOSTICS] Connecting to SMTP server at {host}:{port}...");
                await smtp.ConnectAsync(host, port, secureOption);
                Console.WriteLine("[SMTP DIAGNOSTICS] Connection established successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP DIAGNOSTICS] Connection failed! Error: {ex.ToString()}");
                throw;
            }

            try
            {
                Console.WriteLine($"[SMTP DIAGNOSTICS] Authenticating as {senderEmail}...");
                await smtp.AuthenticateAsync(senderEmail, _config["EmailSettings:Password"]!);
                Console.WriteLine("[SMTP DIAGNOSTICS] Authentication succeeded!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP DIAGNOSTICS] Authentication failed! Error: {ex.ToString()}");
                throw;
            }

            try
            {
                Console.WriteLine($"[SMTP DIAGNOSTICS] Sending email to {toEmail}...");
                await smtp.SendAsync(email);
                Console.WriteLine("[SMTP DIAGNOSTICS] Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP DIAGNOSTICS] Sending failed! Error: {ex.ToString()}");
                throw;
            }
            finally
            {
                try
                {
                    Console.WriteLine("[SMTP DIAGNOSTICS] Disconnecting SMTP client...");
                    await smtp.DisconnectAsync(true);
                    Console.WriteLine("[SMTP DIAGNOSTICS] SMTP client disconnected.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SMTP DIAGNOSTICS] Disconnect threw exception (ignored): {ex.Message}");
                }
            }
        }
    }
}