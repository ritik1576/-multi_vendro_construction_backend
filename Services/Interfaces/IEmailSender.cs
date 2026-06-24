using System.Threading.Tasks;

namespace InframartAPI_New.Services.Interfaces
{
    public class EmailResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
        public string? ProviderResponse { get; set; }
        public string? MessageId { get; set; }
    }

    public interface IEmailSender
    {
        Task<EmailResult> SendEmailAsync(string to, string subject, string htmlBody);
    }
}
