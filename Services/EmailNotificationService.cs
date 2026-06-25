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
            if (string.IsNullOrWhiteSpace(templateKey))
            {
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
