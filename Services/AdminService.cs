using InframartAPI_New.Data;
using InframartAPI_New.DTOs;
using InframartAPI_New.DTOs.Auth;
using InframartAPI_New.DTOs.VendorDTOs;
using InframartAPI_New.Models;
using InframartAPI_New.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InframartAPI_New.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;
        private readonly ApplicationDbContext _appContext;
        private readonly IConfiguration _configuration;

        public AdminService(AppDbContext context, ApplicationDbContext appContext, IConfiguration configuration)
        {
            _context = context;
            _appContext = appContext;
            _configuration = configuration;
        }


        public async Task<(bool success, string? error, List<VendorStatusDto>? data)> GetAllVendorsAsync()
        {
            var vendors = await _context.Vendors.ToListAsync();
            var userIds = vendors.Where(v => v.UserId.HasValue).Select(v => v.UserId!.Value).Distinct().ToList();
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

            var vendorList = vendors.Select(vendor =>
            {
                users.TryGetValue(vendor.UserId ?? 0, out var user);
                return new VendorStatusDto
                {
                    UserId = vendor.UserId ?? 0,
                    VendorId = vendor.Id,
                    ShopName = vendor.ShopName,
                    ShopSlug = vendor.ShopSlug,
                    Description = vendor.Description,
                    Logo = vendor.Logo,
                    Banner = vendor.Banner,
                    GstNumber = vendor.GstNumber,
                    CommissionRate = vendor.CommissionRate,
                    Status = vendor.Status,
                    CreatedAt = vendor.CreatedAt,
                    UpdatedAt = vendor.UpdatedAt,
                    Email = user?.Email,
                    Phone = user?.Phone
                };
            }).ToList();

            return (true, null, vendorList);
        }

        public async Task<(bool success, string? error)> UpdateVendorStatusAsync(long vendorId, string status)
        {
            var allowedStatuses = new[] { "pending", "active", "suspended", "rejected" };
            var newStatus = status?.ToLower() ?? "";

            if (!allowedStatuses.Contains(newStatus))
            {
                return (false, $"Invalid status. Allowed values are: {string.Join(", ", allowedStatuses)}");
            }

            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId);
            if (vendor == null)
            {
                return (false, "Vendor not found");
            }

            vendor.Status = newStatus;
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool success, string? error, List<UserResponseDto>? data)> GetAllUsersAsync()
        {
            var users = await _context.Users.ToListAsync();

            var orderCounts = await _appContext.Orders
                .GroupBy(o => o.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var userDtos = users.Select(u => new UserResponseDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role,
                Status = u.Status,
                OrderCount = orderCounts.TryGetValue(u.Id, out var count) ? count : 0
            }).ToList();

            return (true, null, userDtos);
        }

        public async Task<(bool success, string? error, UserDetailsResponseDto? data)> GetUserDetailsAsync(long userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return (false, "User not found", null);
            }

            var addresses = await _appContext.Addresses
                .Where(a => a.UserId == userId)
                .Select(a => new AddressDto
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    Phone = a.Phone,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    City = a.City,
                    State = a.State,
                    Country = a.Country,
                    PostalCode = a.PostalCode,
                    AddressType = a.AddressType,
                    IsDefault = a.IsDefault,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            var orders = await _appContext.Orders
                .Where(o => o.UserId == userId)
                .Select(o => new AdminOrderResponseDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Subtotal = o.Subtotal,
                    TotalAmount = o.TotalAmount,
                    DiscountAmount = o.DiscountAmount,
                    ShippingCharge = o.ShippingCharge,
                    PaymentStatus = o.PaymentStatus,
                    OrderStatus = o.OrderStatus,
                    PlacedAt = o.PlacedAt,
                    CreatedAt = o.CreatedAt,
                    UserId = o.UserId,
                    CustomerName = user.FullName,
                    CustomerEmail = user.Email
                })
                .ToListAsync();

            var details = new UserDetailsResponseDto
            {
                User = new UserResponseDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role,
                    Status = user.Status
                },
                Addresses = addresses,
                Orders = orders
            };

            return (true, null, details);
        }

        public async Task<(bool success, string? error)> UpdateUserStatusAsync(long userId, string status)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            string targetStatus;
            if (string.Equals(status, "toggle", StringComparison.OrdinalIgnoreCase))
            {
                targetStatus = string.Equals(user.Status, "suspended", StringComparison.OrdinalIgnoreCase) ? "active" : "suspended";
            }
            else
            {
                var allowedStatuses = new[] { "active", "suspended", "inactive" };
                targetStatus = status?.ToLower() ?? "";

                if (!allowedStatuses.Contains(targetStatus))
                {
                    return (false, $"Invalid status. Allowed values are: {string.Join(", ", allowedStatuses)} or 'toggle'");
                }
            }

            user.Status = targetStatus;
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool success, string? error, AdminVendorDetailsDto? data)> GetVendorDetailsAsync(long vendorId)
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId);
            if (vendor == null)
            {
                return (false, "Vendor not found", null);
            }

            User? user = null;
            if (vendor.UserId.HasValue)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == vendor.UserId.Value);
            }

            var details = new AdminVendorDetailsDto
            {
                VendorId = vendor.Id,
                UserId = vendor.UserId ?? 0,
                ShopName = vendor.ShopName,
                ShopSlug = vendor.ShopSlug,
                Description = vendor.Description,
                Logo = vendor.Logo,
                Banner = vendor.Banner,
                GstNumber = vendor.GstNumber,
                CommissionRate = vendor.CommissionRate,
                Status = vendor.Status,
                CreatedAt = vendor.CreatedAt,
                UpdatedAt = vendor.UpdatedAt,
                VendorName = user?.FullName,
                VendorEmail = user?.Email,
                VendorPhone = user?.Phone
            };

            return (true, null, details);
        }

        public async Task<(bool success, string? error, List<AdminOrderResponseDto>? data)> GetAllOrdersAsync()
        {
            var orders = await _appContext.Orders.ToListAsync();
            var userIds = orders.Select(o => o.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var orderList = orders.Select(o =>
            {
                users.TryGetValue(o.UserId, out var user);
                return new AdminOrderResponseDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Subtotal = o.Subtotal,
                    TotalAmount = o.TotalAmount,
                    DiscountAmount = o.DiscountAmount,
                    ShippingCharge = o.ShippingCharge,
                    PaymentStatus = o.PaymentStatus,
                    OrderStatus = o.OrderStatus,
                    PlacedAt = o.PlacedAt,
                    CreatedAt = o.CreatedAt,
                    UserId = o.UserId,
                    CustomerName = user?.FullName,
                    CustomerEmail = user?.Email
                };
            }).ToList();

            return (true, null, orderList);
        }

        public async Task<(bool success, string? error, AuthResponseDto? data)> LoginAdminAsync(LoginDto dto)
        {
            if (dto == null)
            {
                return (false, "Invalid request", null);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || string.IsNullOrEmpty(user.Password) ||
                !PasswordHelper.VerifyPassword(dto.Password, user.Password))
            {
                return (false, "Invalid email or password", null);
            }

            if (user.Role != "admin")
            {
                return (false, "Access denied. Admin role required.", null);
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
                    Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"] ?? "120")
                ),
                signingCredentials: creds
            );

            var response = new AuthResponseDto
            {
                Success = true,
                Message = "Admin login successful",
                UserId = user.Id,
                Role = user.Role,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };

            return (true, null, response);
        }
    }
}

