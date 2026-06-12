using InframartAPI_New.DTOs;
using InframartAPI_New.DTOs.Auth;
using InframartAPI_New.DTOs.VendorDTOs;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IAdminService
    {
        Task<(bool success, string? error, List<VendorStatusDto>? data)> GetAllVendorsAsync();
        Task<(bool success, string? error)> UpdateVendorStatusAsync(long vendorId, string status);
        Task<(bool success, string? error, List<UserResponseDto>? data)> GetAllUsersAsync();
        Task<(bool success, string? error, UserDetailsResponseDto? data)> GetUserDetailsAsync(long userId);
        Task<(bool success, string? error)> UpdateUserStatusAsync(long userId, string status);
        Task<(bool success, string? error, AdminVendorDetailsDto? data)> GetVendorDetailsAsync(long vendorId);
        Task<(bool success, string? error, List<AdminOrderResponseDto>? data)> GetAllOrdersAsync();
        Task<(bool success, string? error, AuthResponseDto? data)> LoginAdminAsync(LoginDto dto);
    }
}
