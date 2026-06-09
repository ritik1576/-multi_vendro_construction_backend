using InframartAPI_New.DTOs;
using InframartAPI_New.Data;
using InframartAPI_New.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InframartAPI_New.Controllers
{
    [ApiController]
    [Route("auth/vendor")]
    public class VendorAuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public VendorAuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ================= REGISTER =================
        [HttpPost("register")]
        public async Task<IActionResult> Register(VendorRegisterDto dto)
        {
            var existing = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (existing != null)
                return BadRequest("Email already exists");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "vendor",
                Status = "active"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var vendor = new Vendor
            {
                Name = dto.ShopName,
                Status = "approved",
                UserId = user.Id
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                 Success = true,
                message = "Vendor registered",
                vendorId = vendor.Id,
                userId = user.Id
            });
        }

        // ================= LOGIN =================
        [HttpPost("login")]
        public async Task<IActionResult> Login(VendorLoginDto dto)

        {
            var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.Email == dto.Email && x.Role == "vendor");

            if (user == null)
                return Unauthorized("Invalid credentials");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password ?? ""))
                return Unauthorized("Invalid credentials");

            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == user.Id);

            var token = GenerateToken(user, vendor?.Id ?? 0);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "Login success",
                Token = token,
                UserId = user.Id,
                VendorId = vendor?.Id ?? 0,
                Role = user.Role,
                Status = vendor?.Status,
                
            });
        }

        // ================= JWT =================
        private string GenerateToken(User user, long vendorId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "vendor"),
                new Claim("vendorId", vendorId.ToString())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken( 
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
