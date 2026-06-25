using System.Collections.Generic;
using System.Threading.Tasks;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IEmailNotificationService
    {
        Task<bool> SendTemplateEmailAsync(string templateKey, string email, Dictionary<string, string> variables);
    }
}
