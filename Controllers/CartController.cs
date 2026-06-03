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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCart(long id)
        {

            var response = await _cartService.GetCartByUserIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {

            var response = await _cartService.AddToCartAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("items")]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemDto dto)
        {

            var response = await _cartService.UpdateCartItemAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("items/delete/{id}")]
        public async Task<IActionResult> RemoveFromCart(long id)
        {

            var response = await _cartService.RemoveFromCartAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("items/{id}")]
        public async Task<IActionResult> ClearCart(long userId)
        {
            var response = await _cartService.ClearCartAsync(userId);
            return StatusCode(response.StatusCode, response);
        }

    }
}
