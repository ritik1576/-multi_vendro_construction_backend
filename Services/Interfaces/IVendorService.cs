using InframartAPI_New.DTOs;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IVendorService
    {
        Task<string> RegisterVendor(VendorRegisterDto dto);

        Task<AuthResponseDto?> LoginVendor(VendorLoginDto dto);
    }
}