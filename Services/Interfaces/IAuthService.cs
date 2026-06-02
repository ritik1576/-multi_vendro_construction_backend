using InframartAPI_New.DTOs.Auth;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IAuthService
    {
        AuthResponseDto Register(RegisterDto dto);
        AuthResponseDto Login(LoginDto dto);
    }
}