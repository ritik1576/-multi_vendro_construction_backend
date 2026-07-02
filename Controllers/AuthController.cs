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
using InframartAPI_New.Services.Interfaces;

namespace InframartAPI_New.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IVendorService _vendorService;
        private readonly INotificationService _notificationService;
        private readonly IEmailNotificationService _emailNotificationService;

        public AuthController(
            AppDbContext context,
            IConfiguration configuration,
            IVendorService vendorService,
            INotificationService notificationService,
            IEmailNotificationService emailNotificationService)
        {
            _context = context;
            _configuration = configuration;
            _vendorService = vendorService;
            _notificationService = notificationService;
            _emailNotificationService = emailNotificationService;
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

            // Create customer wallet
            var wallet = new Wallet
            {
                UserId = user.Id,
                WalletType = WalletType.Customer,
                AvailableBalance = 0.00m,
                LockedBalance = 0.00m,
                TotalCredits = 0.00m,
                TotalDebits = 0.00m,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RowVersion = 1
            };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            // Trigger welcome notification
            await _notificationService.CreateNotificationAsync(user.Id, "Welcome to InfraMart", "Welcome to InfraMart!", "customer");

            // Send WELCOME_EMAIL
            try
            {
                await _emailNotificationService.SendTemplateEmailAsync(
                    "WELCOME_EMAIL",
                    user.Email ?? "",
                    new Dictionary<string, string> { { "customer_name", user.FullName ?? "Customer" } }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send welcome email: {ex.Message}");
            }

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
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email!),
                new Claim(ClaimTypes.Role, user.Role!)
            };

            // Custom checks for vendor role
            Vendor? vendor = null;
            if (string.Equals(user.Role, "vendor", StringComparison.OrdinalIgnoreCase))
            {
                vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == user.Id);
                if (vendor == null)
                {
                    return Unauthorized(new DTOs.AuthResponseDto
                    {
                        Success = false,
                        Message = "Vendor profile not found."
                    });
                }

                // Check KYC Status
                if (vendor.KycStatus == KycStatus.NotSubmitted)
                {
                    return Ok(new DTOs.AuthResponseDto
                    {
                        Success = false,
                        RequiresKyc = true,
                        Message = "Please complete KYC verification."
                    });
                }
                else if (vendor.KycStatus == KycStatus.Submitted || vendor.KycStatus == KycStatus.UnderReview)
                {
                    return Ok(new DTOs.AuthResponseDto
                    {
                        Success = false,
                        Message = "Your KYC is under review."
                    });
                }
                else if (vendor.KycStatus == KycStatus.Rejected)
                {
                    var kyc = await _context.VendorKycs.FirstOrDefaultAsync(k => k.VendorId == vendor.Id);
                    return Ok(new DTOs.AuthResponseDto
                    {
                        Success = false,
                        Message = "KYC rejected.",
                        RejectionReason = kyc?.RejectionReason ?? "Invalid GST Certificate"
                    });
                }

                // Check Vendor Approval Status
                if (vendor.Status == VendorStatus.Pending)
                {
                    return Ok(new DTOs.AuthResponseDto
                    {
                        Success = false,
                        Message = "Your account is awaiting admin approval."
                    });
                }
                else if (vendor.Status == VendorStatus.Rejected)
                {
                    return Ok(new DTOs.AuthResponseDto
                    {
                        Success = false,
                        Message = "Vendor account rejected."
                    });
                }

                claims.Add(new Claim("vendorId", vendor.Id.ToString()));
            }

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
                Success = true,
                Message = "Login successful",
                UserId = user.Id,
                VendorId = vendor?.Id ?? 0,
                Role = user.Role,
                FullName = user.FullName,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                VendorStatus = vendor?.Status.ToString(),
                KycStatus = vendor?.KycStatus.ToString()
            };

            return Ok(response);
        }
        [HttpPost("vendor/register")]
        [HttpPost("/vendor/register")]
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
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
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
                 Success = true,
                 Message = "Admin login successful",
                 UserId = user.Id,
                 Role = user.Role,
                 FullName = user.FullName,
                 Token = new JwtSecurityTokenHandler().WriteToken(token)
             };

            return Ok(response);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new { message = "Email and New Password are required." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return NotFound(new { message = "User not found." });

            user.Password = Services.PasswordHelper.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            // Trigger password reset notification
            await _notificationService.CreateNotificationAsync(user.Id, "Password Reset Successful", "Your password has been reset successfully.", "system");

            // Send PASSWORD_RESET email
            try
            {
                await _emailNotificationService.SendTemplateEmailAsync(
                    "PASSWORD_RESET",
                    user.Email ?? "",
                    new Dictionary<string, string>
                    {
                        { "CustomerName", user.FullName ?? "Customer" },
                        { "ResetLink", "https://inframart.com/login" }
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send password reset email: {ex.Message}");
            }

            return Ok(new { message = "Password reset successfully." });
        }
    }
}












