using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MultiVendorAPI.Models
{
    [Table("addresses")]
    public class Address
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public long Id { get; set; }

        [Column("user_id")]
        public long? UserId { get; set; }

        [Column("full_name")]
        public string? FullName { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("address_line1")]
        public string? AddressLine1 { get; set; }

        [Column("address_line2")]
        public string? AddressLine2 { get; set; }

        [Column("city")]
        public string? City { get; set; }

        [Column("state")]
        public string? State { get; set; }

        [Column("country")]
        public string? Country { get; set; }

        [Column("postal_code")]
        public string? PostalCode { get; set; }

        [Column("address_type")]
        public string? AddressType { get; set; } // 'home' or 'office'

        [Column("is_default")]
        public bool IsDefault { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
