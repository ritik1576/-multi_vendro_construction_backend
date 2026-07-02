using MultiVendorAPI.Common;
using MultiVendorAPI.DTOs.AddressDTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiVendorAPI.Services.Interfaces
{
    public interface IAddressService
    {
        Task<ServiceResponse<AddressResponseDto>> GetAddressByIdAsync(long id);
        Task<ServiceResponse<List<AddressResponseDto>>> GetAddressesByUserIdAsync(long userId);
        Task<ServiceResponse<AddressResponseDto>> CreateAddressAsync(CreateAddressDto dto);
        Task<ServiceResponse<AddressResponseDto>> UpdateAddressAsync(long id, UpdateAddressDto dto);
        Task<ServiceResponse<bool>> DeleteAddressAsync(long id);
    }
}
