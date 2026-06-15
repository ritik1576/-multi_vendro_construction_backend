using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MultiVendorAPI.Models
{
    [Table("image_files")]
    public class ImageFile
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("storage_key")]
        [StringLength(500)]
        public string StorageKey { get; set; } = string.Empty;

        [Required]
        [Column("file_name")]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [Column("content_type")]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Column("vendor_id")]
        public long VendorId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
