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

        public async Task<ServiceResponse<CartDto>> GetCartByUserIdAsync(long userId)
        {
            Console.WriteLine($"UserId received: {userId}");

            var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);

            if (cart == null)
            {
                return ServiceResponse<CartDto>.FailureResponse("Cart not found", 404);
            }

            return ServiceResponse<CartDto>.SuccessResponse(MapToDto(cart));
        }
        public async Task<ServiceResponse<AddToCartResponseDto>> AddToCartAsync(AddToCartDto dto)
        {
            long userId = dto.userId;

            Console.WriteLine($"DTO UserId = {dto.userId}");
            Console.WriteLine($"Local UserId = {userId}");

            if (string.IsNullOrWhiteSpace(dto.ProductName) || dto.Quantity <= 0)
            {
                return ServiceResponse<AddToCartResponseDto>
                    .FailureResponse("Invalid cart item data", 400);
            }

            var product = await _cartRepository.GetProductByNameAsync(dto.ProductName);

            if (product == null)
            {
                return ServiceResponse<AddToCartResponseDto>
                    .FailureResponse("Product not found", 404);
            }

            var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId
                };

                await _cartRepository.AddCartAsync(cart);
                await _cartRepository.SaveChangesAsync();
            }

            CartItem? cartItem;

            var existingItem = cart.CartItems
                .FirstOrDefault(ci => ci.ProductId == product.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;

                await _cartRepository.UpdateCartItemAsync(existingItem);

                cartItem = existingItem;
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = product.Id,
                    Quantity = dto.Quantity
                };

                await _cartRepository.AddCartItemAsync(cartItem);
            }

            await _cartRepository.SaveChangesAsync();

            var response = new AddToCartResponseDto
            {
                ProductName = product.Name,
                Quantity = cartItem.Quantity,
                userId = userId,
                CartItemId = cartItem.Id,
                CartId = cart.Id
            };

            return ServiceResponse<AddToCartResponseDto>
                .SuccessResponse(
                    response,
                    "Item added to cart"
                );
        }

        public async Task<ServiceResponse<CartDto>> UpdateCartItemAsync(UpdateCartItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductName) || dto.Quantity <= 0)
            {
                return ServiceResponse<CartDto>
                    .FailureResponse("Invalid cart item data", 400);
            }

            var cartItem = await _cartRepository.GetCartItemByIdAsync(dto.CartItemId);

            if (cartItem == null)
            {
                return ServiceResponse<CartDto>
                    .FailureResponse("Cart item not found", 404);
            }

            var product = await _cartRepository.GetProductByNameAsync(dto.ProductName);

            if (product == null)
            {
                return ServiceResponse<CartDto>
                    .FailureResponse("Product not found", 404);
            }

            cartItem.ProductId = product.Id;
            cartItem.Quantity = dto.Quantity;

            await _cartRepository.UpdateCartItemAsync(cartItem);
            await _cartRepository.SaveChangesAsync();

            var updatedCart = await _cartRepository.GetByUserIdWithItemsAsync(dto.userId);

            return ServiceResponse<CartDto>
                .SuccessResponse(MapToDto(updatedCart), "Cart item updated");
        }

        public async Task<ServiceResponse<string>> RemoveFromCartAsync(long cartItemId)
        {
            var cartItem = await _cartRepository.GetCartItemByIdAsync(cartItemId);

            if (cartItem == null)
            {
                return ServiceResponse<string>.FailureResponse(
                    "Cart item not found",
                    404);
            }

            await _cartRepository.RemoveCartItemAsync(cartItem);
            await _cartRepository.SaveChangesAsync();

            return ServiceResponse<string>.SuccessResponse(
                cartItemId.ToString(),
                "Item removed from cart",
                200);
        }
        public async Task<ServiceResponse<string>> ClearCartAsync(long userId)
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
                CartItemId = ci.Id,
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
