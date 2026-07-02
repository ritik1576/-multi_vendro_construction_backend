using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InframartAPI_New.Models
{
    [Table("email_template_variables")]
    public class EmailTemplateVariable
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("template_setting_id")]
        public long TemplateSettingId { get; set; }

        [ForeignKey("TemplateSettingId")]
        public EmailTemplateSetting? TemplateSetting { get; set; }

        [Required]
        [Column("variable_key")]
        public string VariableKey { get; set; } = string.Empty;

        [Required]
        [Column("variable_value")]
        public string VariableValue { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
