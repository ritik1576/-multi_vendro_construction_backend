using InframartAPI_New.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IVendorKycService
    {
        Task<(bool success, string message)> SubmitKycAsync(long userId, KycSubmitDto dto);
        Task<(bool success, string? error, KycStatusResponseDto? data)> GetKycStatusAsync(long userId);
        
        // Admin operations
        Task<(bool success, string? error, List<AdminKycDetailsDto>? data)> GetKycRequestsAsync(string? statusFilter);
        Task<(bool success, string? error, AdminKycDetailsDto? data)> GetKycDetailsAsync(long vendorId);
        Task<(bool success, string message)> ApproveKycAsync(long vendorId, long adminUserId);
        Task<(bool success, string message)> RejectKycAsync(long vendorId, string reason, long adminUserId);
        
        // Vendor approvals
        Task<(bool success, string message)> ApproveVendorAsync(long vendorId);
        Task<(bool success, string message)> RejectVendorAsync(long vendorId, string reason);
    }
}
