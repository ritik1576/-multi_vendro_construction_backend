using InframartAPI_New.DTOs;
public interface IVendorService
{
    Task<AuthResponseDto> RegisterVendorAsync(
        VendorRegisterDto dto
    );

    Task<AuthResponseDto> LoginVendorAsync(
        VendorLoginDto dto
    );
}