using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InframartAPI_New.Services.Interfaces;

namespace InframartAPI_New.Services
{
    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly IEmailService _emailService;

        private static readonly Dictionary<string, (string Path, string DefaultSubject)> TemplateMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            { "WELCOME_EMAIL", ("Welcome/Welcome", "Welcome to InfraMart!") },
            { "ORDER_CREATED", ("Orders/OrderPlaced", "Your Order Has Been Placed Successfully!") },
            { "ORDER_CANCELLED", ("Orders/OrderCancelled", "Your Order Has Been Cancelled") },
            { "ORDER_DELIVERED", ("Orders/OrderDelivered", "Your Order Has Been Delivered") },
            { "VENDOR_APPROVED", ("Vendor/VendorApproved", "Your Vendor Profile Has Been Approved!") },
            { "VENDOR_REJECTED", ("Vendor/VendorRejected", "Your Vendor Profile Request Status") },
            { "KYC_APPROVED", ("KYC/Approved", "Your Vendor KYC Has Been Approved!") },
            { "KYC_REJECTED", ("KYC/Rejected", "Your Vendor KYC Has Been Rejected") },
            { "FORGOT_PASSWORD", ("Password/ForgotPassword", "Reset Your Password") },
            { "PASSWORD_RESET", ("Password/ForgotPassword", "Your Password Has Been Reset Successfully") },
            { "OTP_EMAIL", ("Password/OTP", "Your One-Time Password (OTP)") },
            { "OTP", ("Password/OTP", "Your One-Time Password (OTP)") },
            { "ADD_MONEY", ("Wallet/AddMoney", "Money Added to Wallet Successfully") },
            { "WITHDRAW", ("Wallet/Withdraw", "Wallet Withdrawal Request Update") },
            { "TRANSFER", ("Wallet/Transfer", "Wallet Money Transfer Notification") },
            { "PAYMENT_SUCCESS", ("Payment/PaymentSuccess", "Payment Successful") },
            { "PAYMENT_FAILED", ("Payment/PaymentFailed", "Payment Failed") },
            { "REFUND_COMPLETED", ("Payment/RefundCompleted", "Refund Completed Successfully") }
        };

        public EmailNotificationService(IEmailService emailService)
        {
            _emailService = emailService;
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

            if (!TemplateMappings.TryGetValue(templateKey, out var mapping))
            {
                // Fallback: if not mapped, treat templateKey itself as path and use generic subject
                mapping = (templateKey, $"Notification from InfraMart: {templateKey}");
            }

            // Route to central Email Service
            return await _emailService.SendTemplateAsync(email, mapping.DefaultSubject, mapping.Path, variables);
        }
    }
}
