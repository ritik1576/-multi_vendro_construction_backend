using InframartAPI_New.Data;
using InframartAPI_New.DTOs;
using InframartAPI_New.Models;
using InframartAPI_New.Repositories.Interfaces;
using InframartAPI_New.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace InframartAPI_New.Services
{
    public class VendorService : IVendorService
    {
        private readonly IVendorRepository _vendorRepository;
        private readonly IConfiguration _configuration;

        public VendorService(
            IVendorRepository vendorRepository,
            IConfiguration configuration)
        {
            _vendorRepository = vendorRepository;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterVendorAsync(
            VendorRegisterDto dto)
        {
            var exists = await _vendorRepository
                .EmailExistsAsync(dto.Email);

            if (exists)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Email already exists"
                };
            }

            var user = new User
            {

                Email = dto.Email,
                Phone = dto.Phone,
                Password = PasswordHelper.HashPassword(dto.Password),
                Role = "vendor",
                Status = "active"
            };

            await _vendorRepository.AddUserAsync(user);
            await _vendorRepository.SaveChangesAsync();

            var vendor = new Vendor
            {
                UserId = user.Id,
                ShopName = dto.ShopName,
                ShopSlug = dto.ShopSlug,
                Description = dto.Description,
                GstNumber = dto.GstNumber,
                Status = "pending"
            };

            await _vendorRepository.AddVendorAsync(vendor);
            await _vendorRepository.SaveChangesAsync();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email!),
                new Claim(ClaimTypes.Role, user.Role!),
                new Claim("vendorId", vendor.Id.ToString())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!
                ));

            var creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new AuthResponseDto
            {
                Success = true,
                Message = "Vendor registered successfully",

                UserId = user.Id,

                VendorId = vendor.Id,

                Role = user.Role,

                Status = vendor.Status,

                ShopName = vendor.ShopName,

                Token = new JwtSecurityTokenHandler()
                    .WriteToken(token)
            };
        }

        public async Task<AuthResponseDto> LoginVendorAsync(VendorLoginDto dto)
        {
            var (user, vendor) = await _vendorRepository
                .GetVendorUserByEmailAsync(dto.Email);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            if (!string.Equals(user.Status ?? "active", "active", StringComparison.OrdinalIgnoreCase))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Your account is not active. Please contact support."
                };
            }

            if (vendor == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Vendor profile not found for this user."
                };
            }

            if (!string.Equals(vendor.Status, "approved", StringComparison.OrdinalIgnoreCase))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = $"Your vendor account is not approved. Current status: {vendor.Status ?? "pending"}"
                };
            }

            if (string.IsNullOrEmpty(user.Password) ||
                !PasswordHelper.VerifyPassword(dto.Password, user.Password))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email!),
                new Claim(ClaimTypes.Role, user.Role!),
                new Claim("vendorId", vendor?.Id.ToString() ?? "")
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login successful",
                UserId = user.Id,
                VendorId = vendor?.Id ?? 0,
                Role = user.Role,
                Status = vendor?.Status,
                ShopName = vendor?.ShopName,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };
        }
    }
}