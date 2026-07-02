using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InframartAPI_New.Models;

namespace MultiVendorAPI.Models
{
    [Table("reviews")]
    public class Review
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Column("product_id")]
        public long ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Column("rating")]
        public int Rating { get; set; }

        [Column("review")]
        public string ReviewText { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
