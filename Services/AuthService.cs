using InframartAPI_New.DTOs;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IAuthService
    {
        // ================= USER =================

        Task<AuthResponseDto> LoginUserAsync(LoginDto dto);

        Task<AuthResponseDto> RegisterUserAsync(RegisterDto dto);

        // ================= VENDOR =================

        Task<AuthResponseDto> LoginVendorAsync(VendorLoginDto dto);

        Task<AuthResponseDto> RegisterVendorAsync(VendorRegisterDto dto);

        // ================= TOKEN =================

        Task<bool> ValidateTokenAsync(string token);
    }
}