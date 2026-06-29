using MultiVendorAPI.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class Order
{

    public long Id { get; set; }
    public DateTime OrderDate { get; set; }
    public long AddressId { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public long? CouponId { get; set; }

    [NotMapped]
    public string? CouponCode { get; set; }
    public string? OrderNumber { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingCharge { get; set; }
    public string PaymentStatus { get; set; } = "pending";
    public string OrderStatus { get; set; } = "pending";
    public DateTime PlacedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long UserId { get; set; }

    [Column("subtotal_amount")]
    public decimal? SubtotalAmount { get; set; }

    [Column("commission_amount")]
    public decimal? CommissionAmount { get; set; }

    [Column("vendor_amount")]
    public decimal? VendorAmount { get; set; }

    [Column("final_amount")]
    public decimal? FinalAmount { get; set; }

    [Column("razorpay_order_id")]
    public string? RazorpayOrderId { get; set; }

    [Column("razorpay_payment_id")]
    public string? RazorpayPaymentId { get; set; }

    [Column("razorpay_signature")]
    public string? RazorpaySignature { get; set; }

    [Column("payment_gateway")]
    public string? PaymentGateway { get; set; }

    [Column("payment_method")]
    public string? PaymentMethod { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }
}
