using System;

namespace MultiVendorAPI.Models
{
    public class CouponUsage
    {
        public long Id { get; set; }
        public long? CouponId { get; set; }
        public long? UserId { get; set; }
        public long? OrderId { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
