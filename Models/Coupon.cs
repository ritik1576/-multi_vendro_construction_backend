using System;

namespace MultiVendorAPI.Models
{
    public class Coupon
    {
        public long Id { get; set; }
        public long? VendorId { get; set; }
        public string? Code { get; set; }
        public string? DiscountType { get; set; } // 'percentage' or 'fixed'
        public decimal? DiscountValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int? UsedCount { get; set; }
        public int? PerUserLimit { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; } 

        public DateTime CreatedAt { get; set; }
    }
}
