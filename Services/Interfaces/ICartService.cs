using MultiVendorAPI.Common;
using MultiVendorAPI.DTOs.CartDTOs;

namespace MultiVendorAPI.Services.Interfaces
{
    public interface ICartService
    {
        Task<ServiceResponse<CartDto>> GetCartByUserIdAsync(string userId);

        Task<ServiceResponse<CartDto>> AddToCartAsync(AddToCartDto dto);

        Task<ServiceResponse<CartDto>> UpdateCartItemAsync(string userId, UpdateCartItemDto dto);

        Task<ServiceResponse<string>> RemoveFromCartAsync(string userId, string productName);

        Task<ServiceResponse<string>> ClearCartAsync(string userId);
    }
}
