using System.Collections.Generic;
using System.Threading.Tasks;
using InframartAPI_New.Models;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IEmailTemplateService
    {
        Task<EmailTemplateSetting?> GetTemplateAsync(string templateKey);
        Task<(string subject, string body)> RenderTemplateAsync(string templateKey, Dictionary<string, string> variables, string recipientEmail = "");
    }
}
