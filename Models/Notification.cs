using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InframartAPI_New.Models
{
    [Table("notifications")]
    public class Notification
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [Column("title")]
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Column("message")]
        [Required]
        public string Message { get; set; } = string.Empty;

        [Column("type")]
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [Column("is_read")]
        public bool IsRead { get; set; }

        [Column("reference_type")]
        [MaxLength(100)]
        public string? ReferenceType { get; set; }

        [Column("reference_id")]
        [MaxLength(100)]
        public string? ReferenceId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
