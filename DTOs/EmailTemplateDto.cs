using System;
using System.ComponentModel.DataAnnotations;

namespace InframartAPI_New.DTOs
{
    public class EmailTemplateCreateDto
    {
        [Required(ErrorMessage = "Template key is required.")]
        public string TemplateKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Template name is required.")]
        public string TemplateName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "HTML content is required.")]
        public string HtmlContent { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public class EmailTemplateUpdateDto
    {
        [Required(ErrorMessage = "Template name is required.")]
        public string TemplateName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "HTML content is required.")]
        public string HtmlContent { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public class EmailTemplateResponseDto
    {
        public long Id { get; set; }
        public string TemplateKey { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class EmailTemplateStatusDto
    {
        public bool IsActive { get; set; }
    }
}
