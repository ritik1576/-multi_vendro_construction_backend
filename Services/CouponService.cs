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

        public async Task<List<CouponDto>> GetAvailableCouponsAsync()
        {
            var now = DateTime.Now;
            return await _context.Coupons
                .Where(c => c.IsActive && !c.IsDeleted && c.StartDate <= now && c.EndDate >= now)
                .Select(c => new CouponDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Title = c.Title,
                    DiscountType = c.DiscountType,
                    DiscountValue = c.DiscountValue
                })
                .ToListAsync();
        }

        public async Task<CouponValidationResult> ValidateCouponAsync(string couponCode, long userId)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return new CouponValidationResult { Valid = false, Message = "Coupon code cannot be empty" };
            }

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToLower() == couponCode.ToLower() && !c.IsDeleted);

            if (coupon == null)
            {
                return new CouponValidationResult { Valid = false, Message = "Coupon does not exist" };
            }

            if (!coupon.IsActive)
            {
                return new CouponValidationResult { Valid = false, Message = "Coupon is inactive" };
            }

            var now = DateTime.Now;
            if (coupon.StartDate > now)
            {
                return new CouponValidationResult { Valid = false, Message = "Coupon promotion has not started yet" };
            }

            if (coupon.EndDate < now)
            {
                return new CouponValidationResult { Valid = false, Message = "Coupon has expired" };
            }

            // Usage Limit Not Exceeded
            if (coupon.UsageLimit.HasValue)
            {
                var totalUsages = await _context.CouponUsages.CountAsync(cu => cu.CouponId == coupon.Id);
                if (totalUsages >= coupon.UsageLimit.Value)
                {
                    return new CouponValidationResult { Valid = false, Message = "Coupon usage limit has been exceeded" };
                }
            }

            // Per User Usage Limit Not Exceeded
            if (coupon.PerUserLimit.HasValue)
            {
                var userUsages = await _context.CouponUsages.CountAsync(cu => cu.CouponId == coupon.Id && cu.UserId == userId);
                if (userUsages >= coupon.PerUserLimit.Value)
                {
                    return new CouponValidationResult { Valid = false, Message = "You have exceeded the usage limit for this coupon" };
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
                    var price = item.Product?.Price ?? 0;
                    cartTotal += price * item.Quantity;
                }
            }

            if (coupon.MinimumAmount.HasValue && cartTotal < coupon.MinimumAmount.Value)
            {
                return new CouponValidationResult 
                { 
                    Valid = false, 
                    Message = $"Minimum cart amount of ₹{coupon.MinimumAmount.Value} required to apply this coupon" 
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

            if (string.Equals(coupon.DiscountType, "Fixed", StringComparison.OrdinalIgnoreCase))
            {
                discount = coupon.DiscountValue;
            }
            else if (string.Equals(coupon.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
            {
                discount = cartTotal * (coupon.DiscountValue / 100m);
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
            if (coupon == null || !coupon.IsActive || coupon.IsDeleted)
            {
                return false;
            }

            if (coupon.PerUserLimit.HasValue)
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
                Title = dto.Title,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MaxDiscount = dto.MaxDiscount,
                MinimumAmount = dto.MinimumAmount,
                UsageLimit = dto.UsageLimit,
                PerUserLimit = dto.PerUserLimit,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _context.Coupons.AddAsync(coupon);
            await _context.SaveChangesAsync();
            return coupon;
        }
    }
}
