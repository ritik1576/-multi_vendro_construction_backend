using InframartAPI_New.DTOs.Auth;
using InframartAPI_New.Helpers;
using InframartAPI_New.Services.Interfaces;

namespace InframartAPI_New.Services
{
    public class AuthService : IAuthService
    {
        public AuthResponseDto Register(RegisterDto dto)
        {
            return new AuthResponseDto
            {
                Message = "Success",
                Email = dto.Email,
                Role = "User",
                Token = JwtHelper.GenerateToken(dto.Email, "User", "THIS_IS_SECRET_KEY_123")
            };
        }

        public AuthResponseDto Login(LoginDto dto)
        {
            return new AuthResponseDto
            {
                Message = "Login Success",
                UserId = 1,
                Email = dto.Email,
                Role = "User",
                Token = JwtHelper.GenerateToken(dto.Email, "User", "THIS_IS_SECRET_KEY_123")
            };
        }
    }
}