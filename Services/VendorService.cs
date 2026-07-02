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
        private readonly INotificationService _notificationService;

        public VendorService(
            IVendorRepository vendorRepository,
            IConfiguration configuration,
            INotificationService notificationService)
        {
            _vendorRepository = vendorRepository;
            _configuration = configuration;
            _notificationService = notificationService;
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
                Status = VendorStatus.Pending,
                KycStatus = KycStatus.NotSubmitted
            };

            await _vendorRepository.AddVendorAsync(vendor);
            await _vendorRepository.SaveChangesAsync();

            // Trigger notification to admins
            try
            {
                var adminUserIds = await _vendorRepository.GetAdminUserIdsAsync();
                foreach (var adminId in adminUserIds)
                {
                    await _notificationService.CreateNotificationAsync(adminId, "New Vendor Registration", $"A new vendor '{vendor.ShopName}' has registered and is pending approval.", "vendor");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Notification failed for vendor registration: {ex.Message}");
            }

            return new AuthResponseDto
            {
                Success = true,
                Message = "Vendor registered successfully. Please complete KYC.",
                UserId = user.Id,
                VendorId = vendor.Id,
                Role = user.Role,
                Status = vendor.Status.ToString(),
                ShopName = vendor.ShopName
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

            if (string.IsNullOrEmpty(user.Password) ||
                !PasswordHelper.VerifyPassword(dto.Password, user.Password))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            // Check KYC Status
            if (vendor.KycStatus == KycStatus.NotSubmitted)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    RequiresKyc = true,
                    Message = "Please complete KYC verification."
                };
            }
            else if (vendor.KycStatus == KycStatus.Submitted || vendor.KycStatus == KycStatus.UnderReview)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Your KYC is under review."
                };
            }
            else if (vendor.KycStatus == KycStatus.Rejected)
            {
                var kyc = await _vendorRepository.GetKycByVendorIdAsync(vendor.Id);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "KYC rejected.",
                    RejectionReason = kyc?.RejectionReason ?? "Invalid GST Certificate"
                };
            }

            // Check Vendor Approval Status
            if (vendor.Status == VendorStatus.Pending)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Your account is awaiting admin approval."
                };
            }
            else if (vendor.Status == VendorStatus.Rejected)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Vendor account rejected."
                };
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email!),
                new Claim(ClaimTypes.Role, user.Role!),
                new Claim("vendorId", vendor.Id.ToString())
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
                VendorId = vendor.Id,
                Role = user.Role,
                Status = vendor.Status.ToString(),
                ShopName = vendor.ShopName,
                FullName = user.FullName,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                VendorStatus = vendor.Status.ToString(),
                KycStatus = vendor.KycStatus.ToString()
            };
        }
    }
}