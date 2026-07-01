using Microsoft.AspNetCore.Http;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IFileUploadService
    {
        Task<InframartAPI_New.DTOs.ProductImageUploadResult> UploadProductImageAsync(IFormFile file);
        Task<string> UploadKycDocumentAsync(IFormFile file);
    }
}
