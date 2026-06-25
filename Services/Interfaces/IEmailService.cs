using System.Collections.Generic;
using System.Threading.Tasks;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendAsync(string to, string subject, string htmlBody);
        Task<bool> SendTemplateAsync(string to, string subject, string templatePath, Dictionary<string, string> variables);
    }
}
