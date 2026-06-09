using InframartAPI_New.Models;
using DbPayment = InframartAPI_New.Models.Payment;
using InframartAPI_New.Data;
using InframartAPI_New.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;

namespace InframartAPI_New.Controllers
{
    [ApiController]
    [Route("payments")]
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

        // ================= CREATE PAYMENT =================
        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto request)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(x => x.Id == request.Order_Id);

            if (order == null)
                return NotFound("Order not found");

            decimal total_amount = order.Total_Amount ?? 0m;

            var client = new RazorpayClient(_razorpay.KeyId, _razorpay.KeySecret);

            Dictionary<string, object> options = new()
            {
                { "amount", Convert.ToInt32(total_amount * 100) },
                { "currency", "INR" },
                { "receipt", order.Order_Number ?? $"ORD-{order.Id}" }
            };

            var razorpayOrder = client.Order.Create(options);

            var payment = new DbPayment
            {
                Order_Id = order.Id,
                User_Id = order.User_Id,
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
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentDto request)
        {
            if (string.IsNullOrEmpty(request.RazorpayPayment_Id))
                return BadRequest("Payment Id missing");

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
                return NotFound("Payment not found");

            dbPayment.Razorpay_Payment_Id = request.RazorpayPayment_Id;
            dbPayment.Status = "paid";
            dbPayment.Paid_At = DateTime.UtcNow;

            var order = await _context.Orders.FindAsync(request.Order_Id);

            if (order != null)
                order.Payment_Status = "paid";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment verified successfully",
                status = "paid"
            });
        }

        // ================= PAYMENT HISTORY =================
        [HttpGet("history")]
        public async Task<IActionResult> PaymentHistory()
        {
            var payments = await _context.Payments
                .OrderByDescending(x => x.Created_At)
                .ToListAsync();

            return Ok(payments);
        }

        // ================= REFUND PAYMENT =================
        [HttpPost("refund")]
        public async Task<IActionResult> RefundPayment([FromBody] RefundDto request)
        {
            var client = new RazorpayClient(_razorpay.KeyId, _razorpay.KeySecret);

            var dbPayment = await _context.Payments
                .FirstOrDefaultAsync(x => x.Razorpay_Payment_Id == request.RazorpayPaymentId);

            if (dbPayment == null)
                return NotFound("Payment not found");

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
                    order.Payment_Status = "refunded";

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
    
