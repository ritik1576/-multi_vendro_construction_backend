using Microsoft.AspNetCore.Http;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IFileUploadService
    {
        Task<string> UploadProductImageAsync(IFormFile file);
    }
}
