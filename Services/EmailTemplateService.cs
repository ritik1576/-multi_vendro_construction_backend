using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace InframartAPI_New.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _senderEmail;

        public EmailTemplateService(IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _webHostEnvironment = webHostEnvironment;
            // Get sender email from settings, default to inframart102@gmail.com if not configured
            _senderEmail = configuration["EmailSettings:SenderEmail"] ?? "inframart102@gmail.com";
        }

        public async Task<string> GetRenderedTemplateAsync(string templatePath, Dictionary<string, string> variables)
        {
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                throw new ArgumentException("Template path cannot be null or empty.", nameof(templatePath));
            }

            // Standardize suffix
            if (!templatePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                templatePath += ".html";
            }

            // Resolve path to the specific email template
            string contentRoot = _webHostEnvironment.ContentRootPath;
            string templateFullPath = Path.Combine(contentRoot, "EmailTemplates", templatePath);

            if (!File.Exists(templateFullPath))
            {
                // Fallback to AppContext.BaseDirectory or current directory if ContentRootPath didn't work as expected
                templateFullPath = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", templatePath);
                if (!File.Exists(templateFullPath))
                {
                    templateFullPath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", templatePath);
                    if (!File.Exists(templateFullPath))
                    {
                        throw new FileNotFoundException($"Email template not found at path: {templateFullPath}");
                    }
                }
            }

            // Resolve path to base layout
            string layoutFullPath = Path.Combine(Path.GetDirectoryName(templateFullPath) ?? contentRoot, "..", "Layouts", "BaseLayout.html");
            layoutFullPath = Path.GetFullPath(layoutFullPath); // Normalize the .. path

            if (!File.Exists(layoutFullPath))
            {
                layoutFullPath = Path.Combine(contentRoot, "EmailTemplates", "Layouts", "BaseLayout.html");
                if (!File.Exists(layoutFullPath))
                {
                    layoutFullPath = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "Layouts", "BaseLayout.html");
                    if (!File.Exists(layoutFullPath))
                    {
                        layoutFullPath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "Layouts", "BaseLayout.html");
                    }
                }
            }

            string baseLayout = "";
            if (File.Exists(layoutFullPath))
            {
                baseLayout = await File.ReadAllTextAsync(layoutFullPath);
            }
            else
            {
                // Fallback layout if layout file is missing
                baseLayout = "<!DOCTYPE html><html><body>{{EmailBody}}</body></html>";
            }

            string templateContent = await File.ReadAllTextAsync(templateFullPath);

            // Merge variables dictionary with standard variables if not already overridden
            var allVariables = variables != null ? new Dictionary<string, string>(variables, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (!allVariables.ContainsKey("SupportEmail"))
            {
                allVariables["SupportEmail"] = _senderEmail;
            }
            if (!allVariables.ContainsKey("CurrentYear"))
            {
                allVariables["CurrentYear"] = DateTime.UtcNow.Year.ToString();
            }

            // Replace placeholders in the specific template first
            templateContent = ReplacePlaceholders(templateContent, allVariables);

            // Inject the rendered template content into the base layout
            string finalHtml = baseLayout.Replace("{{EmailBody}}", templateContent, StringComparison.OrdinalIgnoreCase);

            // Replace placeholders in the layout (like {{CurrentYear}} or {{SupportEmail}})
            finalHtml = ReplacePlaceholders(finalHtml, allVariables);

            return finalHtml;
        }

        private string ReplacePlaceholders(string content, Dictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(content)) return content;

            foreach (var variable in variables)
            {
                string value = variable.Value ?? string.Empty;
                
                // Replace {{VariableName}}
                string doubleBracePlaceholder = "{{" + variable.Key + "}}";
                content = Regex.Replace(content, Regex.Escape(doubleBracePlaceholder), value, RegexOptions.IgnoreCase);

                // Replace {VariableName}
                string singleBracePlaceholder = "{" + variable.Key + "}";
                content = Regex.Replace(content, Regex.Escape(singleBracePlaceholder), value, RegexOptions.IgnoreCase);
            }

            return content;
        }
    }
}
