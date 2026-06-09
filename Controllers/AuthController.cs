using InframartAPI_New.DTOs;
using InframartAPI_New.Data;
using InframartAPI_New.DTOs.Auth;
using InframartAPI_New.Helpers;
using InframartAPI_New.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InframartAPI_New.Controllers
{
    [Route("auth/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ================= REGISTER =================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            if (request == null)
                return BadRequest(new { message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) ||                      
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "All fields are required" });
            }

            var userExists = await _context.Users
                .AnyAsync(x => x.Email == request.Email);

            if (userExists)
                return BadRequest(new { message = "User already exists" });

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                Password = PasswordHelper.HashPassword(request.Password),
                Role = "customer"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User registered successfully",
                userId = user.Id
            });
        }

       

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            if (request == null)
                return BadRequest(new { message = "Invalid request" });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || string.IsNullOrEmpty(user.Password) ||
                !PasswordHelper.VerifyPassword(request.Password, user.Password))
            {
                return Unauthorized(new AuthResponseDto
                {
                    Message = "Invalid email or password"
                });
            }

            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            if (!PasswordHelper.VerifyPassword(request.Password, user.Password))
                return Unauthorized(new { message = "Invalid email or password" });

            var claims = new List<Claim>
            {
        new Claim(ClaimTypes.Name, user.Email!),
        new Claim(ClaimTypes.Role, user.Role!)
    };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"])
                ),
                signingCredentials: creds
            );

            var response = new AuthResponseDto
            {
                Message = "Login successful",
                UserId = user.Id,
                Role = user.Role,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };

            return Ok(response);
        }
    }
}











