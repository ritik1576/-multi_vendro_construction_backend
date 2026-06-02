using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorAPI.DTOs.CartDTOs;
using MultiVendorAPI.Services.Interfaces;
using System.Security.Claims;

namespace MultiVendorAPI.Controllers
{
    [ApiController]
    [Route("cart")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            var response = await _cartService.GetCartByUserIdAsync(userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var userId = GetUserId();
            var response = await _cartService.AddToCartAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("items")]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemDto dto)
        {
            var userId = GetUserId();
            var response = await _cartService.UpdateCartItemAsync(userId, dto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("items/{productName}")]
        public async Task<IActionResult> RemoveFromCart(string productName)
        {
            var userId = GetUserId();
            var response = await _cartService.RemoveFromCartAsync(userId, productName);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetUserId();
            var response = await _cartService.ClearCartAsync(userId);
            return StatusCode(response.StatusCode, response);
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty;
        }
    }
}
