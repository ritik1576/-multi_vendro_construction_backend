using System.Threading.Tasks;

namespace InframartAPI_New.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}