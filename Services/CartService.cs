using MultiVendorAPI.Common;
using MultiVendorAPI.DTOs.CartDTOs;
using MultiVendorAPI.Models;
using MultiVendorAPI.Repositories.Interfaces;
using MultiVendorAPI.Services.Interfaces;

namespace MultiVendorAPI.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;

        public CartService(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public async Task<ServiceResponse<CartDto>> GetCartByUserIdAsync(string userId)
        {
            var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);

            if (cart == null)
            {
                return ServiceResponse<CartDto>.FailureResponse("Cart not found", 404);
            }

            return ServiceResponse<CartDto>.SuccessResponse(MapToDto(cart));
        }

        public async Task<ServiceResponse<CartDto>> AddToCartAsync(AddToCartDto dto)
        {
            string userId = dto.userId.ToString();
            if (string.IsNullOrWhiteSpace(dto.ProductName) || dto.Quantity <= 0)
            {
                return ServiceResponse<CartDto>.FailureResponse("Invalid cart item data", 400);
            }

            var product = await _cartRepository.GetProductByNameAsync(dto.ProductName);
            if (product == null)
            {
                return ServiceResponse<CartDto>.FailureResponse("Product not found", 404);
            }

            var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                await _cartRepository.AddCartAsync(cart);
                await _cartRepository.SaveChangesAsync();
            }

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == product.Id);
            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
                await _cartRepository.UpdateCartItemAsync(existingItem);
            }
            else
            {
                await _cartRepository.AddCartItemAsync(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = product.Id,
                    Quantity = dto.Quantity
                });
            }

            await _cartRepository.SaveChangesAsync();

            var updatedCart = await _cartRepository.GetByUserIdWithItemsAsync(userId);
            return ServiceResponse<CartDto>.SuccessResponse(
                MapToDto(updatedCart!),
                "Item added to cart",
                200);
        }

        public async Task<ServiceResponse<CartDto>> UpdateCartItemAsync(string userId, UpdateCartItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductName) || dto.Quantity <= 0)
            {
                return ServiceResponse<CartDto>.FailureResponse("Invalid cart item data", 400);
            }

            var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);
            if (cart == null)
            {
                return ServiceResponse<CartDto>.FailureResponse("Cart not found", 404);
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci =>
                ci.Product?.Name != null &&
                ci.Product.Name.Equals(dto.ProductName, StringComparison.OrdinalIgnoreCase));

            if (cartItem == null)
            {
                return ServiceResponse<CartDto>.FailureResponse("Cart item not found", 404);
            }

            cartItem.Quantity = dto.Quantity;
            await _cartRepository.UpdateCartItemAsync(cartItem);
            await _cartRepository.SaveChangesAsync();

            var updatedCart = await _cartRepository.GetByUserIdWithItemsAsync(userId);
            return ServiceResponse<CartDto>.SuccessResponse(
                MapToDto(updatedCart!),
                "Cart item updated",
                200);
        }

        public async Task<ServiceResponse<string>> RemoveFromCartAsync(string userId, string productName)
        {
            var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);
            if (cart == null)
            {
                return ServiceResponse<string>.FailureResponse("Cart not found", 404);
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci =>
                ci.Product?.Name != null &&
                ci.Product.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));

            if (cartItem == null)
            {
                return ServiceResponse<string>.FailureResponse("Cart item not found", 404);
            }

            await _cartRepository.RemoveCartItemAsync(cartItem);
            await _cartRepository.SaveChangesAsync();

            return ServiceResponse<string>.SuccessResponse(
                productName,
                "Item removed from cart",
                200);
        }

        public async Task<ServiceResponse<string>> ClearCartAsync(string userId)
        {
            var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);
            if (cart == null)
            {
                return ServiceResponse<string>.FailureResponse("Cart not found", 404);
            }

            if (cart.CartItems.Count == 0)
            {
                return ServiceResponse<string>.SuccessResponse(string.Empty, "Cart is already empty", 200);
            }

            await _cartRepository.RemoveCartItemsAsync(cart.CartItems);
            await _cartRepository.SaveChangesAsync();

            return ServiceResponse<string>.SuccessResponse(string.Empty, "Cart cleared", 200);
        }

        private static CartDto MapToDto(Cart cart)
        {
            var items = cart.CartItems.Select(ci => new CartItemDto
            {
                ProductName = ci.Product?.Name ?? string.Empty,
                Quantity = ci.Quantity,
                Price = ci.Product?.Price
            }).ToList();

            return new CartDto
            {
                UserId = cart.UserId,
                Items = items,
                TotalPrice = items.Sum(i => i.TotalPrice)
            };
        }
    }
}
