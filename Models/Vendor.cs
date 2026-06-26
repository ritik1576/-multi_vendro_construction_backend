using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace InframartAPI_New.Models
{
    [Table("Vendors")]
    public class Vendor
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("user_id")]
        public long? UserId { get; set; }

        [Column("shop_name")]
        public string? ShopName { get; set; }

        [Column("shop_slug")]
        public string? ShopSlug { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("logo")]
        public string? Logo { get; set; }

        [Column("banner")]
        public string? Banner { get; set; }

        [Column("gst_number")]
        public string? GstNumber { get; set; }

        [Column("commission_rate")]
        public decimal? CommissionRate { get; set; }

        [Column("status")]
        public VendorStatus Status { get; set; } = VendorStatus.Pending;

        [Column("kyc_status")]
        public KycStatus KycStatus { get; set; } = KycStatus.NotSubmitted;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}