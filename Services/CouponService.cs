using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Data;
using MultiVendorAPI.Models;
using MultiVendorAPI.DTOs;
using MultiVendorAPI.Services.Interfaces;
using MultiVendorAPI.Repositories.Interfaces;

namespace MultiVendorAPI.Services
{
    public class CouponService : ICouponService
    {
        private readonly ApplicationDbContext _context;

        public CouponService(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GenerateCouponTitle(string code, string discountType, decimal discountValue)
        {
            if (string.Equals(discountType, "percentage", StringComparison.OrdinalIgnoreCase))
            {
                return $"{code} - {discountValue:0.##}% OFF";
            }
            return $"{code} - ₹{discountValue:0.##} OFF";
        }

        private DateTime GetLocalTime()
        {
            try
            {
                var timeZoneId = OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata";
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                // Fallback to UTC + 5:30 (India Standard Time offset)
                return DateTime.UtcNow.AddHours(5).AddMinutes(30);
            }
        }

        public async Task<List<CouponDto>> GetAvailableCouponsAsync()
        {
            var now = GetLocalTime();
            var coupons = await _context.Coupons
                .Where(c => c.Status == "active" && c.StartDate <= now && c.EndDate >= now)
                .ToListAsync();

            return coupons.Select(c => new CouponDto
            {
                Id = c.Id,
                Code = c.Code ?? string.Empty,
                Title = GenerateCouponTitle(c.Code ?? string.Empty, c.DiscountType ?? "fixed", c.DiscountValue ?? 0),
                DiscountType = string.Equals(c.DiscountType, "percentage", StringComparison.OrdinalIgnoreCase) ? "Percentage" : "Fixed",
                DiscountValue = c.DiscountValue ?? 0
            }).ToList();
        }

        public async Task<CouponValidationResult> ValidateCouponAsync(string couponCode, long userId)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return new CouponValidationResult { Valid = false, Message = "Coupon code cannot be empty" };
            }

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code != null && c.Code.ToLower() == couponCode.ToLower());

            if (coupon == null)
            {
                return new CouponValidationResult { Valid = false, Message = "Coupon does not exist" };
            }

            if (coupon.Status != "active")
            {
                return new CouponValidationResult { Valid = false, Message = "Coupon is inactive" };
            }

            var now = GetLocalTime();
            if ((coupon.StartDate.HasValue) && (coupon.StartDate.Value > now))
            {
                return new CouponValidationResult { Valid = false, Message = "Coupon promotion has not started yet" };
            }

            if (coupon.EndDate.HasValue && coupon.EndDate.Value < now)
            {
                return new CouponValidationResult { Valid = false, Message = "Coupon has expired" };
            }

            // Usage Limit Not Exceeded
            if (coupon.UsageLimit.HasValue && coupon.UsageLimit.Value > 0)
            {
                var totalUsages = await _context.CouponUsages.CountAsync(cu => cu.CouponId == coupon.Id);
                if (totalUsages >= coupon.UsageLimit.Value)
                {
                    return new CouponValidationResult { Valid = false, Message = "Coupon usage limit has been exceeded" };
                }
            }

            // Per User Usage Limit
            if (coupon.PerUserLimit.HasValue && coupon.PerUserLimit.Value > 0)
            {
                var userUsages = await _context.CouponUsages.CountAsync(cu => cu.CouponId == coupon.Id && cu.UserId == userId);
                if (userUsages >= coupon.PerUserLimit.Value)
                {
                    return new CouponValidationResult { Valid = false, Message = "You have reached the usage limit for this coupon" };
                }
            }

            // Cart Minimum Amount Satisfied
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            decimal cartTotal = 0;
            if (cart != null && cart.CartItems != null)
            {
                foreach (var item in cart.CartItems)
                {
                    var price = (item.Product?.DiscountPrice.HasValue == true && item.Product.DiscountPrice.Value > 0)
                        ? item.Product.DiscountPrice.Value
                        : (item.Product?.Price ?? 0);
                    cartTotal += price * item.Quantity;
                }
            }

            if (coupon.MinimumOrderAmount.HasValue && cartTotal < coupon.MinimumOrderAmount.Value)
            {
                return new CouponValidationResult 
                { 
                    Valid = false, 
                    Message = $"Minimum cart amount of ₹{coupon.MinimumOrderAmount.Value} required to apply this coupon" 
                };
            }

            return new CouponValidationResult
            {
                Valid = true,
                Coupon = coupon,
                Message = "Coupon applied successfully"
            };
        }

        public Task<decimal> CalculateDiscountAsync(Coupon coupon, decimal cartTotal)
        {
            if (coupon == null) return Task.FromResult(0m);

            decimal discount = 0;
            decimal val = coupon.DiscountValue ?? 0;

            if (string.Equals(coupon.DiscountType, "fixed", StringComparison.OrdinalIgnoreCase))
            {
                discount = val;
            }
            else if (string.Equals(coupon.DiscountType, "percentage", StringComparison.OrdinalIgnoreCase))
            {
                discount = cartTotal * (val / 100m);
                if (coupon.MaxDiscount.HasValue && discount > coupon.MaxDiscount.Value)
                {
                    discount = coupon.MaxDiscount.Value;
                }
            }

            // Discount cannot exceed cart total
            if (discount > cartTotal)
            {
                discount = cartTotal;
            }

            return Task.FromResult(discount);
        }

        public async Task<bool> CanUserUseCouponAsync(long couponId, long userId)
        {
            var coupon = await _context.Coupons.FindAsync(couponId);
            if (coupon == null || coupon.Status != "active")
            {
                return false;
            }

            // Standard limits check (no per_user_limit column present in table, so we default to true unless usage_limit is hit)
            if (coupon.UsageLimit.HasValue && coupon.UsageLimit.Value > 0)
            {
                var totalUsages = await _context.CouponUsages.CountAsync(cu => cu.CouponId == couponId);
                if (totalUsages >= coupon.UsageLimit.Value)
                {
                    return false;
                }
            }

            if (coupon.PerUserLimit.HasValue && coupon.PerUserLimit.Value > 0)
            {
                var userUsages = await _context.CouponUsages.CountAsync(cu => cu.CouponId == couponId && cu.UserId == userId);
                if (userUsages >= coupon.PerUserLimit.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<Coupon> CreateCouponAsync(CreateCouponDto dto)
        {
            var coupon = new Coupon
            {
                Code = dto.Code.Trim().ToUpper(),
                DiscountType = dto.DiscountType.ToLower() == "percentage" ? "percentage" : "fixed",
                DiscountValue = dto.DiscountValue,
                MaxDiscount = dto.MaxDiscount,
                MinimumOrderAmount = dto.MinimumAmount,
                UsageLimit = dto.UsageLimit,
                PerUserLimit = dto.PerUserLimit,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = "active",
                CreatedAt = DateTime.Now
            };

            await _context.Coupons.AddAsync(coupon);
            await _context.SaveChangesAsync();
            return coupon;
        }

        public async Task<Coupon?> UpdateCouponAsync(long id, CreateCouponDto dto)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return null;
            }

            coupon.Code = dto.Code.Trim().ToUpper();
            coupon.DiscountType = dto.DiscountType.ToLower() == "percentage" ? "percentage" : "fixed";
            coupon.DiscountValue = dto.DiscountValue;
            coupon.MaxDiscount = dto.MaxDiscount;
            coupon.MinimumOrderAmount = dto.MinimumAmount;
            coupon.UsageLimit = dto.UsageLimit;
            coupon.PerUserLimit = dto.PerUserLimit;
            coupon.StartDate = dto.StartDate;
            coupon.EndDate = dto.EndDate;

            _context.Coupons.Update(coupon);
            await _context.SaveChangesAsync();
            return coupon;
        }

        public async Task<bool> DeleteCouponAsync(long id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return false;
            }

            // Check if the coupon is referenced in any orders
            var hasOrders = await _context.Orders.AnyAsync(o => o.CouponId == id);
            if (hasOrders)
            {
                // Soft delete: set status to inactive so order history is preserved
                coupon.Status = "inactive";
                _context.Coupons.Update(coupon);
            }
            else
            {
                // Hard delete: remove usages and the coupon itself
                var usages = await _context.CouponUsages.Where(cu => cu.CouponId == id).ToListAsync();
                if (usages.Any())
                {
                    _context.CouponUsages.RemoveRange(usages);
                }

                _context.Coupons.Remove(coupon);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<AdminCouponDto>> GetAllCouponsForAdminAsync()
        {
            var coupons = await _context.Coupons.ToListAsync();
            return coupons.Select(c => new AdminCouponDto
            {
                Id = c.Id,
                Code = c.Code ?? string.Empty,
                DiscountType = string.Equals(c.DiscountType, "percentage", StringComparison.OrdinalIgnoreCase) ? "Percentage" : "Fixed",
                DiscountValue = c.DiscountValue ?? 0,
                MaxDiscount = c.MaxDiscount,
                MinimumOrderAmount = c.MinimumOrderAmount,
                UsageLimit = c.UsageLimit,
                PerUserLimit = c.PerUserLimit,
                UsedCount = c.UsedCount ?? 0,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status ?? "inactive",
                CreatedAt = c.CreatedAt
            }).ToList();
        }
    }
}
