using MultiVendorAPI.Models;

namespace MultiVendorAPI.DTOs
{
    public class CouponDto
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
    }

    public class ApplyCouponRequestDto
    {
        public string CouponCode { get; set; } = string.Empty;
    }

    public class ApplyCouponResponseDto
    {
        public bool Valid { get; set; }
        public long? CouponId { get; set; }
        public string? CouponCode { get; set; }
        public decimal CartTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CouponValidationResult
    {
        public bool Valid { get; set; }
        public Coupon? Coupon { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CreateCouponDto
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty; // "Fixed" or "Percentage"
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal? MinimumAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int? PerUserLimit { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
