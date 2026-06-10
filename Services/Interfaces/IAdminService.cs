using InframartAPI_New.DTOs.VendorDTOs;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IAdminService
    {
        Task<(bool success, string? error, List<VendorStatusDto>? data)> GetAllVendorsAsync();
        Task<(bool success, string? error)> UpdateVendorStatusAsync(long vendorId, string status);
    }
}
