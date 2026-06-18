using System.Collections.Generic;
using System.Threading.Tasks;
using MultiVendorAPI.Models;
using MultiVendorAPI.DTOs;

namespace MultiVendorAPI.Services.Interfaces
{
    public interface ICouponService
    {
        Task<List<CouponDto>> GetAvailableCouponsAsync();
        Task<CouponValidationResult> ValidateCouponAsync(string couponCode, long userId);
        Task<decimal> CalculateDiscountAsync(Coupon coupon, decimal cartTotal);
        Task<bool> CanUserUseCouponAsync(long couponId, long userId);
        Task<Coupon> CreateCouponAsync(CreateCouponDto dto);
        Task<Coupon?> UpdateCouponAsync(long id, CreateCouponDto dto);
        Task<bool> DeleteCouponAsync(long id);
    }
}
