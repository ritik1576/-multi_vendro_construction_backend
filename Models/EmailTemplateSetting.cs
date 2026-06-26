using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InframartAPI_New.Models
{
    [Table("email_template_settings")]
    public class EmailTemplateSetting
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("template_key")]
        public string TemplateKey { get; set; } = string.Empty;

        [Required]
        [Column("subject")]
        public string Subject { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public ICollection<EmailTemplateVariable> Variables { get; set; } = new List<EmailTemplateVariable>();
    }
}
