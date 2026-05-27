using InframartAPI_New.DTOs.Auth;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IAuthService
    {
        string Register(RegisterDto dto);
        string Login(LoginDto dto);
    }
}