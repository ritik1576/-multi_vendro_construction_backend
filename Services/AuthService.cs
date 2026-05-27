using InframartAPI_New.DTOs.Auth;
using InframartAPI_New.Helpers;
using InframartAPI_New.Services.Interfaces;

namespace InframartAPI_New.Services
{
    public class AuthService : IAuthService
    {
        public string Register(RegisterDto dto)
        {
            if (dto == null)
                return "Invalid request";

            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
                return "Email or Password cannot be empty";

            var hashedPassword = PasswordHasher.Hash(dto.Password);

            // TODO: Save to DB later

            return $"User Registered Successfully: {dto.Email}";
        }

        public string Login(LoginDto dto)
        {
            if (dto == null)
                return "Invalid request";

            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
                return "Email or Password required";

            // TODO: Validate from DB later

            var token = JwtHelper.GenerateToken(
                dto.Email,
                "User",
                "THIS_IS_SECRET_KEY_123"
            );

            return token;
        }
    }
}