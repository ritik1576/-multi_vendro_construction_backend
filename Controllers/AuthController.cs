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
using InframartAPI_New.Services;

namespace InframartAPI_New.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IVendorService _vendorService;

        public AuthController(AppDbContext context, IConfiguration configuration, IVendorService vendorService)
        {
            _context = context;
            _configuration = configuration;
            _vendorService = vendorService;

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
                Password = Services.PasswordHelper.HashPassword(request.Password),
                Role = "customer",
                Status = "active"
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

            if (user == null)
            {
                return Unauthorized(new DTOs.AuthResponseDto
                {
                    Message = "Invalid email or password"
                });
            }

            if (!string.Equals(user.Status ?? "active", "active", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new DTOs.AuthResponseDto
                {
                    Message = "Your account is not active. Please contact support."
                });
            }

            if (string.IsNullOrEmpty(user.Password) ||
                !Services.PasswordHelper.VerifyPassword(request.Password, user.Password))
            {
                return Unauthorized(new DTOs.AuthResponseDto
                {
                    Message = "Invalid email or password"
                });
            }

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

            Console.WriteLine($"DB User Id: {user.Id}");
            Console.WriteLine($"DB Email: {user.Email}");
            var response = new DTOs.AuthResponseDto
            {
                Message = "Login successful",
                UserId = user.Id,
                Role = user.Role,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };

            return Ok(response);
        }
        [HttpPost("vendor/register")]
        public async Task<IActionResult> RegisterVendor(
    [FromBody] VendorRegisterDto dto)
        {
            var result = await _vendorService
                .RegisterVendorAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("vendor/login")]
        public async Task<IActionResult> LoginVendor([FromBody] VendorLoginDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }
            var result = await _vendorService.LoginVendorAsync(dto);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [HttpPost("admin/login")]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginDto request)
        {
            if (request == null)
                return BadRequest(new { message = "Invalid request" });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return Unauthorized(new DTOs.AuthResponseDto
                {
                    Message = "Invalid email or password"
                });
            }

            if (!string.Equals(user.Status ?? "active", "active", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new DTOs.AuthResponseDto
                {
                    Message = "Your account is not active. Please contact support."
                });
            }

            if (string.IsNullOrEmpty(user.Password) ||
                !Services.PasswordHelper.VerifyPassword(request.Password, user.Password))
            {
                return Unauthorized(new DTOs.AuthResponseDto
                {
                    Message = "Invalid email or password"
                });
            }

            if (user.Role != "admin")
            {
                return Unauthorized(new DTOs.AuthResponseDto
                {
                    Message = "Access denied. Admin role required."
                });
            }

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

            var response = new DTOs.AuthResponseDto
            {
                Message = "Admin login successful",
                UserId = user.Id,
                Role = user.Role,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };

            return Ok(response);
        }
    }
}












