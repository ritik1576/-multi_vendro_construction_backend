using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InframartAPI_New.Data;
using InframartAPI_New.DTOs;
using InframartAPI_New.Models;
using InframartAPI_New.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MultiVendorAPI.Common;
using Razorpay.Api;
using DbPayment = InframartAPI_New.Models.Payment;

namespace InframartAPI_New.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;
        private readonly MultiVendorAPI.Data.ApplicationDbContext _appContext;
        private readonly RazorpaySettings _razorpay;
        private readonly INotificationService _notificationService;

        public PaymentService(
            AppDbContext context,
            MultiVendorAPI.Data.ApplicationDbContext appContext,
            IOptions<RazorpaySettings> razorpay,
            INotificationService notificationService)
        {
            _context = context;
            _appContext = appContext;
            _razorpay = razorpay.Value;
            _notificationService = notificationService;
        }

        public async Task<ServiceResponse<object>> CreatePaymentAsync(CreatePaymentDto request, long userId)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(x => x.Id == request.Order_Id);

                if (order == null)
                {
                    return ServiceResponse<object>.FailureResponse("Order not found", 404);
                }

                if (order.UserId != userId)
                {
                    return ServiceResponse<object>.FailureResponse("Cannot make payment for another user's order.", 403);
                }

                decimal total_amount = order.TotalAmount;

                var client = new RazorpayClient(_razorpay.KeyId, _razorpay.KeySecret);

                Dictionary<string, object> options = new()
                {
                    { "amount", Convert.ToInt32(total_amount * 100) },
                    { "currency", "INR" },
                    { "receipt", order.OrderNumber ?? $"ORD-{order.Id}" }
                };

                var razorpayOrder = client.Order.Create(options);

                var payment = new DbPayment
                {
                    Order_Id = order.Id,
                    User_Id = order.UserId,
                    Amount = total_amount,
                    Status = "created",
                    Payment_Method = "razorpay",
                    Created_At = DateTime.Now,
                    Razorpay_Order_Id = razorpayOrder["id"].ToString()
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                var data = new
                {
                    message = "Payment order created",
                    razorpay_order_id = razorpayOrder["id"].ToString(),
                    razorpay_key = _razorpay.KeyId,
                    payment_id = payment.Id
                };

                return ServiceResponse<object>.SuccessResponse(data, "Payment order created successfully", 201);
            }
            catch (Exception ex)
            {
                return ServiceResponse<object>.FailureResponse($"Failed to create payment: {ex.Message}", 500);
            }
        }

        public async Task<ServiceResponse<object>> VerifyPaymentAsync(VerifyPaymentDto request, long userId)
        {
            if (string.IsNullOrEmpty(request.RazorpayPayment_Id))
            {
                return ServiceResponse<object>.FailureResponse("Payment Id missing", 400);
            }

            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(x => x.Id == request.Order_Id);

                if (order == null)
                {
                    return ServiceResponse<object>.FailureResponse("Order not found", 404);
                }

                if (order.UserId != userId)
                {
                    return ServiceResponse<object>.FailureResponse("Cannot verify payment for another user's order.", 403);
                }

                var client = new RazorpayClient(_razorpay.KeyId, _razorpay.KeySecret);
                object paymentResponse;

                try
                {
                    paymentResponse = client.Payment.Fetch(request.RazorpayPayment_Id);

                    if (((Dictionary<string, object>)paymentResponse)["status"].ToString() != "captured")
                    {
                        // Trigger Payment Failed Notification
                        await _notificationService.CreateNotificationAsync(order.UserId, "Payment Failed", $"Payment failed for your order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                        await _notificationService.CreateNotificationAsync(order.UserId, "Order Payment Failed", $"Payment failed for your order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                        return ServiceResponse<object>.FailureResponse("Payment not captured", 400);
                    }
                }
                catch (Exception ex)
                {
                    // Trigger Payment Failed Notification
                    await _notificationService.CreateNotificationAsync(order.UserId, "Payment Failed", $"Payment failed for your order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                    await _notificationService.CreateNotificationAsync(order.UserId, "Order Payment Failed", $"Payment failed for your order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                    return ServiceResponse<object>.FailureResponse($"Invalid Razorpay Payment ID or check failed: {ex.Message}", 400);
                }

                var dbPayment = await _context.Payments
                    .FirstOrDefaultAsync(x => x.Razorpay_Order_Id == request.RazorpayOrder_Id);

                if (dbPayment == null)
                {
                    return ServiceResponse<object>.FailureResponse("Payment record not found", 404);
                }

                dbPayment.Razorpay_Payment_Id = request.RazorpayPayment_Id;
                dbPayment.Status = "paid";
                dbPayment.Paid_At = DateTime.Now;

                order.PaymentStatus = "paid";

                await _context.SaveChangesAsync();

                // Trigger Customer Notifications
                await _notificationService.CreateNotificationAsync(order.UserId, "Payment Successful", $"Payment of {order.TotalAmount} INR was successful for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                await _notificationService.CreateNotificationAsync(order.UserId, "Order Payment Successful", $"Payment of {order.TotalAmount} INR was successful for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());

                // Calculate commission and vendor amounts
                decimal commissionAmount = order.CommissionAmount ?? (order.TotalAmount * 0.10m);
                decimal vendorAmount = order.VendorAmount ?? (order.TotalAmount - commissionAmount);

                // Get Vendor User
                var firstItem = await _appContext.OrderItems.FirstOrDefaultAsync(oi => oi.OrderId == order.Id);
                long? vendorUserId = null;
                if (firstItem != null)
                {
                    var product = await _appContext.Products.FirstOrDefaultAsync(p => p.Id == firstItem.ProductId);
                    if (product != null && product.VendorId.HasValue)
                    {
                        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == product.VendorId.Value);
                        if (vendor != null && vendor.UserId.HasValue)
                        {
                            vendorUserId = vendor.UserId.Value;
                        }
                    }
                }

                // Get Admin User
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "admin");

                // Vendor Notifications
                if (vendorUserId.HasValue)
                {
                    await _notificationService.CreateNotificationAsync(vendorUserId.Value, "Payment Received", $"Payment of {vendorAmount} INR received for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                    await _notificationService.CreateNotificationAsync(vendorUserId.Value, "Commission Deducted", $"Commission of {commissionAmount} INR deducted for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                    await _notificationService.CreateNotificationAsync(vendorUserId.Value, "Order Payment Received", $"Order payment of {vendorAmount} INR received for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                }

                // Admin Notifications
                if (adminUser != null)
                {
                    await _notificationService.CreateNotificationAsync(adminUser.Id, "New Payment Received", $"Payment of {order.TotalAmount} INR received for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                    await _notificationService.CreateNotificationAsync(adminUser.Id, "Commission Received", $"Commission of {commissionAmount} INR received for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                }

                var data = new
                {
                    message = "Payment verified successfully",
                    status = "paid"
                };

                return ServiceResponse<object>.SuccessResponse(data, "Payment verified successfully");
            }
            catch (Exception ex)
            {
                return ServiceResponse<object>.FailureResponse($"An error occurred during verification: {ex.Message}", 500);
            }
        }

        public async Task<ServiceResponse<List<DbPayment>>> PaymentHistoryAsync()
        {
            try
            {
                var payments = await _context.Payments
                    .OrderByDescending(x => x.Created_At)
                    .ToListAsync();

                return ServiceResponse<List<DbPayment>>.SuccessResponse(payments, "Payment history retrieved successfully");
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<DbPayment>>.FailureResponse($"Failed to retrieve payment history: {ex.Message}", 500);
            }
        }

        public async Task<ServiceResponse<object>> RefundPaymentAsync(RefundDto request)
        {
            try
            {
                var client = new RazorpayClient(_razorpay.KeyId, _razorpay.KeySecret);

                var dbPayment = await _context.Payments
                    .FirstOrDefaultAsync(x => x.Razorpay_Payment_Id == request.RazorpayPaymentId);

                if (dbPayment == null)
                {
                    return ServiceResponse<object>.FailureResponse("Payment record not found", 404);
                }

                if (dbPayment.Status == "refunded")
                {
                    return ServiceResponse<object>.FailureResponse("Already refunded", 400);
                }

                var razorpayPayment = client.Payment.Fetch(request.RazorpayPaymentId);

                Dictionary<string, object> options = new()
                {
                    { "amount", Convert.ToInt32(request.Amount * 100) }
                };

                var refund = razorpayPayment.Refund(options);

                dbPayment.Status = "refunded";

                var order = await _context.Orders
                    .FirstOrDefaultAsync(x => x.Id == dbPayment.Order_Id);

                if (order != null)
                {
                    order.PaymentStatus = "refunded";
                }

                await _context.SaveChangesAsync();

                // Trigger Refund Processed and Completed Notifications
                if (dbPayment.User_Id.HasValue)
                {
                    await _notificationService.CreateNotificationAsync(dbPayment.User_Id.Value, "Refund Processed", $"Refund of {request.Amount} INR was successfully processed for your payment.", "payment", "Order", order?.Id.ToString());
                    await _notificationService.CreateNotificationAsync(dbPayment.User_Id.Value, "Refund Completed", $"Refund of {request.Amount} INR has been completed for order {order?.OrderNumber}.", "payment", "Order", order?.Id.ToString());
                }

                // Vendor and Admin notifications
                if (order != null)
                {
                    // Calculate vendor refund deduction
                    decimal commissionRefund = request.Amount * 0.10m;
                    decimal vendorDeduction = request.Amount - commissionRefund;

                    // Get Vendor
                    var firstItem = await _appContext.OrderItems.FirstOrDefaultAsync(oi => oi.OrderId == order.Id);
                    long? vendorUserId = null;
                    if (firstItem != null)
                    {
                        var product = await _appContext.Products.FirstOrDefaultAsync(p => p.Id == firstItem.ProductId);
                        if (product != null && product.VendorId.HasValue)
                        {
                            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == product.VendorId.Value);
                            if (vendor != null && vendor.UserId.HasValue)
                            {
                                vendorUserId = vendor.UserId.Value;
                            }
                        }
                    }

                    if (vendorUserId.HasValue)
                    {
                        await _notificationService.CreateNotificationAsync(vendorUserId.Value, "Refund Deducted", $"Refund deduction of {vendorDeduction} INR was deducted from your wallet for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                    }

                    // Admin notifications
                    var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "admin");
                    if (adminUser != null)
                    {
                        await _notificationService.CreateNotificationAsync(adminUser.Id, "Refund Approved", $"Refund of {request.Amount} INR has been approved for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                        await _notificationService.CreateNotificationAsync(adminUser.Id, "Refund Processed", $"Refund of {request.Amount} INR has been processed for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                    }
                }

                var data = new
                {
                    message = "Refund successful",
                    refund_id = refund["id"].ToString(),
                    status = "refunded"
                };

                return ServiceResponse<object>.SuccessResponse(data, "Refund processed successfully");
            }
            catch (Exception ex)
            {
                return ServiceResponse<object>.FailureResponse($"Refund failed: {ex.Message}", 500);
            }
        }
    }
}
