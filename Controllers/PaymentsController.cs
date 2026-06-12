using InframartAPI_New.Models;
using DbPayment = InframartAPI_New.Models.Payment;
using InframartAPI_New.Data;
using InframartAPI_New.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Razorpay.Api;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InframartAPI_New.Middlewares;

namespace InframartAPI_New.Controllers
{
    [ApiController]
    [Route("payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly RazorpaySettings _razorpay;

        public PaymentsController(
            AppDbContext context,
            IOptions<RazorpaySettings> razorpay)
        {
            _context = context;
            _razorpay = razorpay.Value;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !long.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException();
            return userId;
        }

        // ================= CREATE PAYMENT =================
        [HttpPost]
        [Authorize(Roles = "customer")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto request)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(x => x.Id == request.Order_Id);

            if (order == null)
                throw new NotFoundException("Order not found");

            if (order.UserId != GetCurrentUserId())
            {
                throw new ForbiddenException("Cannot make payment for another user's order.");
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
                Created_At = DateTime.UtcNow,
                Razorpay_Order_Id = razorpayOrder["id"].ToString()
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment order created",
                razorpay_order_id = razorpayOrder["id"].ToString(),
                razorpay_key = _razorpay.KeyId,
                payment_id = payment.Id
            });
        }

        // ================= VERIFY PAYMENT =================
        [HttpPost("verify")]
        [Authorize(Roles = "customer")]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentDto request)
        {
            if (string.IsNullOrEmpty(request.RazorpayPayment_Id))
                return BadRequest("Payment Id missing");

            var order = await _context.Orders
                .FirstOrDefaultAsync(x => x.Id == request.Order_Id);

            if (order == null)
                throw new NotFoundException("Order not found");

            if (order.UserId != GetCurrentUserId())
            {
                throw new ForbiddenException("Cannot verify payment for another user's order.");
            }

            var client = new RazorpayClient(_razorpay.KeyId, _razorpay.KeySecret);
            object paymentResponse;

            try
            {
                paymentResponse = client.Payment.Fetch(request.RazorpayPayment_Id);

                if (((Dictionary<string, object>)paymentResponse)["status"].ToString() != "captured")
                    return BadRequest("Payment not captured");
            }
            catch
            {
                return BadRequest("Invalid Razorpay Payment ID");
            }

            var dbPayment = await _context.Payments
                .FirstOrDefaultAsync(x => x.Razorpay_Order_Id == request.RazorpayOrder_Id);

            if (dbPayment == null)
                throw new NotFoundException("Payment record not found");

            dbPayment.Razorpay_Payment_Id = request.RazorpayPayment_Id;
            dbPayment.Status = "paid";
            dbPayment.Paid_At = DateTime.UtcNow;

            order.PaymentStatus = "paid";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment verified successfully",
                status = "paid"
            });
        }

        // ================= PAYMENT HISTORY =================
        [HttpGet("history")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> PaymentHistory()
        {
            var payments = await _context.Payments
                .OrderByDescending(x => x.Created_At)
                .ToListAsync();

            return Ok(payments);
        }

        // ================= REFUND PAYMENT =================
        [HttpPost("refund")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RefundPayment([FromBody] RefundDto request)
        {
            var client = new RazorpayClient(_razorpay.KeyId, _razorpay.KeySecret);

            var dbPayment = await _context.Payments
                .FirstOrDefaultAsync(x => x.Razorpay_Payment_Id == request.RazorpayPaymentId);

            if (dbPayment == null)
                throw new NotFoundException("Payment record not found");

            if (dbPayment.Status == "refunded")
                return BadRequest("Already refunded");

            try
            {
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
                    order.PaymentStatus = "refunded";

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Refund successful",
                    refund_id = refund["id"].ToString(),
                    status = "refunded"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Refund failed",
                    error = ex.Message
                });
            }
        }
    }
}
