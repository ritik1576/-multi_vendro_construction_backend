using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Data;
using MultiVendorAPI.DTOs;
using MultiVendorAPI.Services.Interfaces;

namespace MultiVendorAPI.Controllers
{
    [ApiController]
    [Route("api/coupons")]
    [Authorize]
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;
        private readonly ApplicationDbContext _context;

        public CouponController(ICouponService couponService, ApplicationDbContext context)
        {
            _couponService = couponService;
            _context = context;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !long.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException();
            return userId;
        }

        [HttpGet]
        [Authorize(Roles = "admin")] // Available coupons can be requested without login or customer role depending on swagger requirements, but only active ones.
        public async Task<IActionResult> GetAvailableCoupons()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("admin"))
            {
                var allCoupons = await _couponService.GetAllCouponsForAdminAsync();
                return Ok(allCoupons);
            }

            var coupons = await _couponService.GetAvailableCouponsAsync();
            return Ok(coupons);
        }

        [HttpPost("apply")]
        [Authorize(Roles = "customer")]
        public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequestDto dto)
        {
            var userId = GetCurrentUserId();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return Ok(new { valid = false, message = "Cart not found for user" });
            }

            decimal cartTotal = 0;
            if (cart.CartItems != null)
            {
                foreach (var item in cart.CartItems)
                {
                    var price = (item.Product?.DiscountPrice.HasValue == true && item.Product.DiscountPrice.Value > 0)
                        ? item.Product.DiscountPrice.Value
                        : (item.Product?.Price ?? 0);
                    cartTotal += price * item.Quantity;
                }
            }

            var validationResult = await _couponService.ValidateCouponAsync(dto.CouponCode, userId);
            if (!validationResult.Valid || validationResult.Coupon == null)
            {
                return Ok(new { valid = false, message = validationResult.Message });
            }

            var discount = await _couponService.CalculateDiscountAsync(validationResult.Coupon, cartTotal);
            decimal shippingCharge = dto.ShippingCharge ?? 99m;
            var finalAmount = cartTotal + shippingCharge - discount;

            return Ok(new ApplyCouponResponseDto
            {
                Valid = true,
                CouponId = validationResult.Coupon.Id,
                CouponCode = validationResult.Coupon.Code,
                CartTotal = cartTotal,
                Subtotal = cartTotal,
                ShippingCharge = shippingCharge,
                DiscountAmount = discount,
                FinalAmount = finalAmount,
                Message = "Coupon applied successfully"
            });
        }

        [HttpPost("remove")]
        [Authorize(Roles = "customer")]
        public IActionResult RemoveCoupon()
        {
            return Ok(new { message = "Coupon removed" });
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.DiscountType))
            {
                return BadRequest(new { message = "Code and DiscountType are required" });
            }

            var existing = await _context.Coupons.AnyAsync(c => c.Code != null && c.Code.ToLower() == dto.Code.ToLower());
            if (existing)
            {
                return BadRequest(new { message = "A coupon with this code already exists" });
            }

            var coupon = await _couponService.CreateCouponAsync(dto);
            return CreatedAtAction(nameof(GetAvailableCoupons), new { id = coupon.Id }, coupon);
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateCoupon(long id, [FromBody] CreateCouponDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.DiscountType))
            {
                return BadRequest(new { message = "Code and DiscountType are required" });
            }

            var existing = await _context.Coupons.AnyAsync(c => c.Id != id && c.Code != null && c.Code.ToLower() == dto.Code.ToLower());
            if (existing)
            {
                return BadRequest(new { message = "Another coupon with this code already exists" });
            }

            var updatedCoupon = await _couponService.UpdateCouponAsync(id, dto);
            if (updatedCoupon == null)
            {
                return NotFound(new { message = "Coupon not found" });
            }

            return Ok(updatedCoupon);
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteCoupon(long id)
        {
            var result = await _couponService.DeleteCouponAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Coupon not found" });
            }

            return Ok(new { message = "Coupon deleted successfully" });
        }
    }
}
