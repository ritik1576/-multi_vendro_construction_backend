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

}
