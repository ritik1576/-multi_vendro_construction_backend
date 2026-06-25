using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InframartAPI_New.Models
{
    [Table("email_logs")]
    public class EmailLog
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("template_id")]
        public long? TemplateId { get; set; }

        [ForeignKey("TemplateId")]
        public EmailTemplate? Template { get; set; }

        [Required]
        [Column("recipient_email")]
        public string RecipientEmail { get; set; } = string.Empty;

        [Required]
        [Column("subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [Column("body")]
        public string Body { get; set; } = string.Empty;

        [Required]
        [Column("status")]
        public string Status { get; set; } = string.Empty; // "Sent" or "Failed"

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("sent_at")]
        public DateTime? SentAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
