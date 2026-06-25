using System.Collections.Generic;
using System.Threading.Tasks;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IEmailTemplateService
    {
        Task<string> GetRenderedTemplateAsync(string templatePath, Dictionary<string, string> variables);
    }
}
