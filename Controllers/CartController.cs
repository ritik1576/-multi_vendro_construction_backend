using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorAPI.DTOs.CartDTOs;
using MultiVendorAPI.Services.Interfaces;
using System.Security.Claims;
using InframartAPI_New.Middlewares;

namespace MultiVendorAPI.Controllers
{
    [ApiController]
    [Route("cart")]
    [Authorize(Roles = "customer")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !long.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException();
            return userId;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCart(long id)
        {
            if (id != GetCurrentUserId())
            {
                throw new ForbiddenException("Cannot access another user's cart.");
            }

            var response = await _cartService.GetCartByUserIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            if (dto.userId != GetCurrentUserId())
            {
                throw new ForbiddenException("Cannot add items to another user's cart.");
            }

            var response = await _cartService.AddToCartAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("update/items")]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemDto dto)
        {
            if (dto.userId != GetCurrentUserId())
            {
                throw new ForbiddenException("Cannot update items in another user's cart.");
            }

            var response = await _cartService.UpdateCartItemAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> RemoveFromCart(long id)
        {
            var response = await _cartService.RemoveFromCartAsync(id, GetCurrentUserId());
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("clear/{userId}")]
        public async Task<IActionResult> ClearCart(long userId)
        {
            if (userId != GetCurrentUserId())
            {
                throw new ForbiddenException("Cannot clear another user's cart.");
            }

            var response = await _cartService.ClearCartAsync(userId);
            return StatusCode(response.StatusCode, response);
        }
    }
}
