using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace InframartAPI_New.Controllers
{
    public class TestSendRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    [Route("api/email")]
    [ApiController]
    [AllowAnonymous] // Allow testing without JWT auth
    public class EmailTestController : ControllerBase
    {
        private readonly IConfiguration _config;

        public EmailTestController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestConnection()
        {
            string host = _config["EmailSettings:SmtpServer"]!;
            int port = int.Parse(_config["EmailSettings:Port"]!);
            
            var secureOption = port == 465 
                ? MailKit.Security.SecureSocketOptions.SslOnConnect 
                : MailKit.Security.SecureSocketOptions.StartTls;

            var result = new Dictionary<string, object>
            {
                { "smtpHost", host },
                { "smtpPort", port },
                { "secureOption", secureOption.ToString() },
                { "resolvedIp", "Not Resolved" },
                { "connectionStatus", "Failed" },
                { "error", null! }
            };

            // Resolve host to IPv4 to prevent IPv6 routing hangs
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host);
                var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                if (ipv4 != null)
                {
                    result["resolvedIp"] = ipv4.ToString();
                    host = ipv4.ToString();
                }
            }
            catch (Exception ex)
            {
                result["error"] = $"DNS Resolution failed: {ex.Message}";
                return StatusCode(500, result);
            }

            using var smtp = new SmtpClient();
            smtp.Timeout = 10000; // 10 seconds
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            try
            {
                await smtp.ConnectAsync(host, port, secureOption);
                result["connectionStatus"] = "Connected";
                await smtp.DisconnectAsync(true);
                return Ok(result);
            }
            catch (Exception ex)
            {
                result["error"] = ex.ToString();
                return StatusCode(500, result);
            }
        }

        [HttpPost("test-send")]
        public async Task<IActionResult> TestSend([FromBody] TestSendRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Email is required" });
            }

            string host = _config["EmailSettings:SmtpServer"]!;
            int port = int.Parse(_config["EmailSettings:Port"]!);
            string senderEmail = _config["EmailSettings:SenderEmail"]!;
            string senderName = _config["EmailSettings:SenderName"]!;

            var secureOption = port == 465 
                ? MailKit.Security.SecureSocketOptions.SslOnConnect 
                : MailKit.Security.SecureSocketOptions.StartTls;

            var result = new Dictionary<string, object>
            {
                { "smtpHost", host },
                { "smtpPort", port },
                { "senderEmail", senderEmail },
                { "recipientEmail", request.Email },
                { "resolvedIp", "Not Resolved" },
                { "connectionResult", "Pending" },
                { "authenticationResult", "Pending" },
                { "sendResult", "Pending" },
                { "error", null! }
            };

            // Resolve host
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host);
                var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                if (ipv4 != null)
                {
                    result["resolvedIp"] = ipv4.ToString();
                    host = ipv4.ToString();
                }
            }
            catch (Exception ex)
            {
                result["error"] = $"DNS Resolution failed: {ex.Message}";
                return StatusCode(500, result);
            }

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(MailboxAddress.Parse(request.Email));
            email.Subject = "InfraMart SMTP Diagnostic Test Email";
            email.Body = new TextPart("html") 
            { 
                Text = $"<h3>SMTP Diagnostic Test</h3><p>Sent at: {DateTime.UtcNow} UTC</p><p>If you see this, email sending is working!</p>" 
            };

            using var smtp = new SmtpClient();
            smtp.Timeout = 10000;
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            try
            {
                // Connect
                await smtp.ConnectAsync(host, port, secureOption);
                result["connectionResult"] = "Success";

                // Authenticate
                await smtp.AuthenticateAsync(senderEmail, _config["EmailSettings:Password"]!);
                result["authenticationResult"] = "Success";

                // Send
                await smtp.SendAsync(email);
                result["sendResult"] = "Success";

                await smtp.DisconnectAsync(true);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (result["connectionResult"].ToString() == "Pending")
                {
                    result["connectionResult"] = "Failed";
                }
                else if (result["authenticationResult"].ToString() == "Pending")
                {
                    result["authenticationResult"] = "Failed";
                }
                else if (result["sendResult"].ToString() == "Pending")
                {
                    result["sendResult"] = "Failed";
                }

                result["error"] = ex.ToString();
                return StatusCode(500, result);
            }
        }
    }
}
