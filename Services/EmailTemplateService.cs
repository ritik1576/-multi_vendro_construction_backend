using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InframartAPI_New.Models;
using InframartAPI_New.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Data;

namespace InframartAPI_New.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly ApplicationDbContext _dbContext;

        public EmailTemplateService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EmailTemplate?> GetTemplateAsync(string templateKey)
        {
            if (string.IsNullOrWhiteSpace(templateKey)) return null;

            return await _dbContext.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateKey == templateKey);
        }

        public async Task<(string subject, string body)> RenderTemplateAsync(string templateKey, Dictionary<string, string> variables)
        {
            var template = await GetTemplateAsync(templateKey);
            if (template == null)
            {
                throw new KeyNotFoundException($"Email template with key '{templateKey}' not found.");
            }

            string renderedSubject = template.Subject;
            string renderedBody = template.HtmlContent;

            if (variables != null)
            {
                foreach (var variable in variables)
                {
                    string keyPlaceholder = "{" + variable.Key + "}";
                    string replacementValue = variable.Value ?? string.Empty;

                    // Case-insensitive replacement
                    renderedSubject = Regex.Replace(renderedSubject, Regex.Escape(keyPlaceholder), replacementValue, RegexOptions.IgnoreCase);
                    renderedBody = Regex.Replace(renderedBody, Regex.Escape(keyPlaceholder), replacementValue, RegexOptions.IgnoreCase);
                }
            }

            return (renderedSubject, renderedBody);
        }
    }
}
