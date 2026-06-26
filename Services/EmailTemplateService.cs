using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InframartAPI_New.Models;
using InframartAPI_New.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Data;

namespace InframartAPI_New.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly ApplicationDbContext _dbContext;

        // Static metadata representing default settings and configurable variables for each template
        public static readonly Dictionary<string, (string Path, string DefaultSubject, Dictionary<string, string> DefaultVars)> TemplateMetadata = new(StringComparer.OrdinalIgnoreCase)
        {
            { "WELCOME_EMAIL", ("Welcome/Welcome", "Welcome to InfraMart!", new() {
                { "HeaderTitle", "Welcome to InfraMart!" },
                { "IntroMessage", "We are thrilled to have you as part of our marketplace. Explore a wide variety of construction materials." },
                { "ButtonText", "Get Started" },
                { "FooterMessage", "Thank you for choosing InfraMart." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "ORDER_CREATED", ("Orders/OrderPlaced", "Your Order Has Been Placed Successfully!", new() {
                { "HeaderTitle", "Order Placed Successfully" },
                { "IntroMessage", "Your order has been received and is being processed by the vendor." },
                { "ButtonText", "View Order" },
                { "FooterMessage", "Thank you for shopping with us." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "ORDER_CANCELLED", ("Orders/OrderCancelled", "Your Order Has Been Cancelled", new() {
                { "HeaderTitle", "Order Cancelled" },
                { "IntroMessage", "We regret to inform you that your order has been cancelled." },
                { "FooterMessage", "If you have any questions, please contact our support." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "ORDER_DELIVERED", ("Orders/OrderDelivered", "Your Order Has Been Delivered", new() {
                { "HeaderTitle", "Order Delivered" },
                { "IntroMessage", "Your order has been delivered successfully to your address." },
                { "ButtonText", "Rate Product" },
                { "FooterMessage", "We hope you are satisfied with your purchase." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "VENDOR_APPROVED", ("Vendor/VendorApproved", "Your Vendor Profile Has Been Approved!", new() {
                { "HeaderTitle", "Vendor Application Approved" },
                { "IntroMessage", "Congratulations! Your vendor profile application has been approved. You can now log in to your dashboard and start selling." },
                { "ButtonText", "Go to Dashboard" },
                { "FooterMessage", "Welcome aboard!" },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "VENDOR_REJECTED", ("Vendor/VendorRejected", "Your Vendor Profile Request Status", new() {
                { "HeaderTitle", "Vendor Application Status" },
                { "IntroMessage", "We regret to inform you that your vendor profile application has been rejected at this time." },
                { "ButtonText", "Resubmit KYC" },
                { "FooterMessage", "Please review the requirements and apply again." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "KYC_APPROVED", ("KYC/Approved", "Your Vendor KYC Has Been Approved!", new() {
                { "HeaderTitle", "KYC Approved" },
                { "IntroMessage", "Your vendor KYC documents have been successfully verified." },
                { "FooterMessage", "Your account is fully active." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "KYC_REJECTED", ("KYC/Rejected", "Your Vendor KYC Has Been Rejected", new() {
                { "HeaderTitle", "KYC Rejected" },
                { "IntroMessage", "Your vendor KYC documents could not be verified." },
                { "ButtonText", "Resubmit KYC" },
                { "FooterMessage", "Please re-submit valid documents on your dashboard." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "FORGOT_PASSWORD", ("Password/ForgotPassword", "Reset Your Password", new() {
                { "HeaderTitle", "Forgot Your Password?" },
                { "IntroMessage", "You requested a password reset. Click the button below to set a new password." },
                { "ButtonText", "Reset Password" },
                { "ExpiryMessage", "This link is valid for 1 hour." },
                { "FooterMessage", "Thank you for using InfraMart." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "PASSWORD_RESET", ("Password/ForgotPassword", "Your Password Has Been Reset Successfully", new() {
                { "HeaderTitle", "Password Reset Successful" },
                { "IntroMessage", "Your password has been reset successfully. If you did not request this, please contact support immediately." },
                { "FooterMessage", "Thank you for using InfraMart." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "OTP_EMAIL", ("Password/OTP", "Your One-Time Password (OTP)", new() {
                { "HeaderTitle", "Your One-Time Password" },
                { "IntroMessage", "Use the following OTP code to verify your action." },
                { "ExpiryMessage", "This OTP is valid for 10 minutes." },
                { "FooterMessage", "Do not share this OTP with anyone." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "OTP", ("Password/OTP", "Your One-Time Password (OTP)", new() {
                { "HeaderTitle", "Your One-Time Password" },
                { "IntroMessage", "Use the following OTP code to verify your action." },
                { "ExpiryMessage", "This OTP is valid for 10 minutes." },
                { "FooterMessage", "Do not share this OTP with anyone." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "ADD_MONEY", ("Wallet/AddMoney", "Money Added to Wallet Successfully", new() {
                { "HeaderTitle", "Wallet Credited" },
                { "IntroMessage", "Money has been successfully credited to your wallet." },
                { "ButtonText", "View Wallet" },
                { "FooterMessage", "Thank you for using our wallet service." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "WITHDRAW", ("Wallet/Withdraw", "Wallet Withdrawal Request Update", new() {
                { "HeaderTitle", "Withdrawal Request Update" },
                { "IntroMessage", "Your withdrawal request has been updated." },
                { "ButtonText", "View Wallet" },
                { "FooterMessage", "If you have any questions, please contact our support." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "TRANSFER", ("Wallet/Transfer", "Wallet Money Transfer Notification", new() {
                { "HeaderTitle", "Wallet Transfer Notification" },
                { "IntroMessage", "A transfer transaction has been processed in your wallet." },
                { "ButtonText", "View Wallet" },
                { "FooterMessage", "Thank you for using our transfer service." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "PAYMENT_SUCCESS", ("Payment/PaymentSuccess", "Payment Successful", new() {
                { "HeaderTitle", "Payment Successful" },
                { "IntroMessage", "Your payment has been successfully processed." },
                { "ButtonText", "Download Invoice" },
                { "FooterMessage", "Thank you for choosing InfraMart." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "PAYMENT_FAILED", ("Payment/PaymentFailed", "Payment Failed", new() {
                { "HeaderTitle", "Payment Failed" },
                { "IntroMessage", "Your payment attempt could not be processed." },
                { "ButtonText", "Retry Payment" },
                { "FooterMessage", "Please try again or use another payment method." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) },
            { "REFUND_COMPLETED", ("Payment/RefundCompleted", "Refund Completed Successfully", new() {
                { "HeaderTitle", "Refund Completed" },
                { "IntroMessage", "Your refund has been successfully completed and credited back to your account." },
                { "FooterMessage", "Thank you for using InfraMart." },
                { "SupportEmail", "support@inframart.com" },
                { "SupportPhone", "+1-800-555-0199" },
                { "CompanyWebsite", "https://inframart.com" },
                { "CompanyLogo", "https://inframart.com/assets/logo.png" },
                { "FacebookUrl", "https://facebook.com/inframart" },
                { "InstagramUrl", "https://instagram.com/inframart" },
                { "TwitterUrl", "https://twitter.com/inframart" },
                { "LinkedInUrl", "https://linkedin.com/company/inframart" }
            }) }
        };

        public EmailTemplateService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EmailTemplateSetting?> GetTemplateAsync(string templateKey)
        {
            if (string.IsNullOrWhiteSpace(templateKey)) return null;

            // Resolve actual key if path is passed (e.g. "Welcome/Welcome" -> "WELCOME_EMAIL")
            var resolvedKey = templateKey.ToUpper();
            var matchedMetadata = TemplateMetadata.FirstOrDefault(m => 
                string.Equals(m.Value.Path, templateKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.Key, templateKey, StringComparison.OrdinalIgnoreCase));

            if (matchedMetadata.Key != null)
            {
                resolvedKey = matchedMetadata.Key.ToUpper();
            }

            var setting = await _dbContext.EmailTemplateSettings
                .Include(s => s.Variables)
                .FirstOrDefaultAsync(s => s.TemplateKey == resolvedKey);

            if (setting == null)
            {
                // Try to seed from defaults
                if (TemplateMetadata.TryGetValue(resolvedKey, out var meta))
                {
                    setting = new EmailTemplateSetting
                    {
                        TemplateKey = resolvedKey,
                        Subject = meta.DefaultSubject,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        Variables = meta.DefaultVars.Select(v => new EmailTemplateVariable
                        {
                            VariableKey = v.Key,
                            VariableValue = v.Value,
                            CreatedAt = DateTime.UtcNow
                        }).ToList()
                    };

                    try
                    {
                        _dbContext.EmailTemplateSettings.Add(setting);
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        // Handle potential concurrent seeding race condition
                        _dbContext.Entry(setting).State = EntityState.Detached;
                        setting = await _dbContext.EmailTemplateSettings
                            .Include(s => s.Variables)
                            .FirstOrDefaultAsync(s => s.TemplateKey == resolvedKey);
                    }
                }
            }

            return setting;
        }

        public async Task<(string subject, string body)> RenderTemplateAsync(string templateKey, Dictionary<string, string> variables, string recipientEmail = "")
        {
            var setting = await GetTemplateAsync(templateKey);
            if (setting == null)
            {
                throw new KeyNotFoundException($"Email template with key '{templateKey}' not found.");
            }

            var resolvedKey = setting.TemplateKey;

            // Find file path from metadata mapping
            var subPath = TemplateMetadata.TryGetValue(resolvedKey, out var meta) ? meta.Path : templateKey;
            var templateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", subPath + ".html");
            var baseLayoutFilePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "Layouts", "BaseLayout.html");

            if (!File.Exists(templateFilePath))
            {
                throw new FileNotFoundException($"Template file not found at path: {templateFilePath}");
            }

            if (!File.Exists(baseLayoutFilePath))
            {
                throw new FileNotFoundException($"Base layout file not found at path: {baseLayoutFilePath}");
            }

            string templateContent = await File.ReadAllTextAsync(templateFilePath);
            string baseLayoutContent = await File.ReadAllTextAsync(baseLayoutFilePath);

            // Merge variables: DB settings first, then overwrite with runtime parameters case-insensitively
            var mergedVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (setting.Variables != null)
            {
                foreach (var v in setting.Variables)
                {
                    mergedVariables[v.VariableKey] = v.VariableValue;
                }
            }

            if (variables != null)
            {
                foreach (var v in variables)
                {
                    mergedVariables[v.Key] = v.Value;
                }
            }

            // Replace Body placeholder in base layout with template body
            var finalHtml = baseLayoutContent.Replace("{{Body}}", templateContent, StringComparison.OrdinalIgnoreCase);

            // Replace Subject placeholder in base layout
            finalHtml = finalHtml.Replace("{{Subject}}", setting.Subject, StringComparison.OrdinalIgnoreCase);

            var renderedSubject = setting.Subject;

            // Replace all placeholders in both subject and HTML body
            foreach (var kvp in mergedVariables)
            {
                string placeholder = "{?" + kvp.Key + "?}"; // support {Variable} or {{Variable}} or {?Variable?} format
                string placeholderDoubleCurly = "{{" + kvp.Key + "}}";
                string placeholderSingleCurly = "{" + kvp.Key + "}";
                string val = kvp.Value ?? string.Empty;

                renderedSubject = renderedSubject
                    .Replace(placeholderDoubleCurly, val, StringComparison.OrdinalIgnoreCase)
                    .Replace(placeholderSingleCurly, val, StringComparison.OrdinalIgnoreCase);

                finalHtml = finalHtml
                    .Replace(placeholder, val, StringComparison.OrdinalIgnoreCase)
                    .Replace(placeholderDoubleCurly, val, StringComparison.OrdinalIgnoreCase)
                    .Replace(placeholderSingleCurly, val, StringComparison.OrdinalIgnoreCase);
            }

            // --- STRICT VERIFICATION & EXCEPTION THROWING ---
            var missingVariablesList = new List<string>();
            var placeholderRegex = new System.Text.RegularExpressions.Regex(@"\{\{([a-zA-Z0-9_]+)\}\}");
            
            // Check Subject for unresolved double-curly placeholders
            var subjectMatches = placeholderRegex.Matches(renderedSubject);
            foreach (System.Text.RegularExpressions.Match match in subjectMatches)
            {
                var varName = match.Groups[1].Value;
                if (!missingVariablesList.Contains(varName, StringComparer.OrdinalIgnoreCase))
                {
                    missingVariablesList.Add(varName);
                }
            }

            // Check final body HTML for unresolved double-curly placeholders
            var bodyMatches = placeholderRegex.Matches(finalHtml);
            foreach (System.Text.RegularExpressions.Match match in bodyMatches)
            {
                var varName = match.Groups[1].Value;
                if (!string.Equals(varName, "Body", StringComparison.OrdinalIgnoreCase) && 
                    !missingVariablesList.Contains(varName, StringComparer.OrdinalIgnoreCase))
                {
                    missingVariablesList.Add(varName);
                }
            }

            // --- LOGGING ---
            // Log final variables and generated URL links
            var generatedUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var urlFields = new[] { "ResetLink", "TrackOrderUrl", "InvoiceUrl", "ReviewProductUrl", "DashboardUrl", "ResubmitUrl", "WalletUrl", "LoginUrl" };
            foreach (var field in urlFields)
            {
                if (mergedVariables.TryGetValue(field, out var urlVal) && !string.IsNullOrWhiteSpace(urlVal))
                {
                    generatedUrls[field] = urlVal;
                }
            }

            // Save generated HTML to path for debug logging
            string tempFilePath = string.Empty;
            try
            {
                var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "debug_emails");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                tempFilePath = Path.Combine(tempDir, $"{resolvedKey}_{Guid.NewGuid():N}.html");
                await File.WriteAllTextAsync(tempFilePath, finalHtml);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailTemplateService] Failed to write debug HTML file: {ex.Message}");
            }

            Console.WriteLine($"[EmailTemplateService] Template Name: {resolvedKey}");
            Console.WriteLine($"[EmailTemplateService] Recipient: {recipientEmail}");
            Console.WriteLine($"[EmailTemplateService] Subject: {renderedSubject}");
            Console.WriteLine($"[EmailTemplateService] Final Variable Dictionary: {System.Text.Json.JsonSerializer.Serialize(mergedVariables)}");
            Console.WriteLine($"[EmailTemplateService] Generated URLs: {System.Text.Json.JsonSerializer.Serialize(generatedUrls)}");
            Console.WriteLine($"[EmailTemplateService] Missing Variables: {System.Text.Json.JsonSerializer.Serialize(missingVariablesList)}");
            Console.WriteLine($"[EmailTemplateService] Generated HTML Path: {tempFilePath}");

            if (missingVariablesList.Count > 0)
            {
                var missingListStr = string.Join(", ", missingVariablesList);
                throw new InvalidOperationException($"Missing runtime variable: {missingListStr} required by template {subPath}.html");
            }

            return (renderedSubject, finalHtml);
        }
    }
}
