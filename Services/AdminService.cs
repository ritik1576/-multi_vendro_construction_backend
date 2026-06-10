using InframartAPI_New.Data;
using InframartAPI_New.DTOs.VendorDTOs;
using InframartAPI_New.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InframartAPI_New.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
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
    }
}
