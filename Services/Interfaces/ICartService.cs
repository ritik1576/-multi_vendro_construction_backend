using MultiVendorAPI.Common;
using MultiVendorAPI.DTOs.CartDTOs;

namespace MultiVendorAPI.Services.Interfaces
{
    public interface ICartService
    {
        Task<ServiceResponse<CartDto>> GetCartByUserIdAsync(long userId);

        Task<ServiceResponse<AddToCartResponseDto>> AddToCartAsync(AddToCartDto dto);

        Task<ServiceResponse<CartDto>> UpdateCartItemAsync(UpdateCartItemDto dto);

        Task<ServiceResponse<string>> RemoveFromCartAsync(long cartItemId, long userId);

        Task<ServiceResponse<string>> ClearCartAsync(long userId);
    }
}
