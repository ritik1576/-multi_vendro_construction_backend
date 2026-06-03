using InframartAPI_New.Data;
using InframartAPI_New.DTOs.Auth;
using InframartAPI_New.Helpers;
using InframartAPI_New.Models;
using InframartAPI_New.Services.Interfaces;
using System.Linq;

namespace InframartAPI_New.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        // ================= REGISTER =================
        public AuthResponseDto Register(RegisterDto dto)
        {
            var userExists = _context.Users
                .FirstOrDefault(x => x.Email == dto.Email);

            if (userExists != null)
            {
                return new AuthResponseDto
                {
                    Message = "User already exists"
                };
            }

            var user = new User
            {
                Name = dto.FullName,
                Email = dto.Email,
                Password = PasswordHelper.HashPassword(dto.Password),
                Role = string.IsNullOrWhiteSpace(dto.Role) ? "customer" : dto.Role,
                Status = "active"
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return new AuthResponseDto
            {
                Message = "Register Success",
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role,
                Token = "dummy-token"
            };
        }

        // ================= LOGIN =================
        public AuthResponseDto Login(LoginDto dto)
        {
            var user = _context.Users
                .FirstOrDefault(x => x.Email == dto.Email);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Message = "Invalid credentials"
                };
            }


#pragma warning disable CS8604 // Possible null reference argument.
            if (!PasswordHelper.VerifyPassword(dto.Password, user.Password))
            {
                return new AuthResponseDto
                {
                    Message = "Invalid credentials"
                };
    

            }
#pragma warning restore CS8604 // Possible null reference argument.


            return new AuthResponseDto
            {
                Message = "Login Success",
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role,
                Token = "dummy-token"
            };
        }
    }
}