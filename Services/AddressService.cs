using MultiVendorAPI.Common;
using MultiVendorAPI.DTOs.AddressDTOs;
using MultiVendorAPI.Models;
using MultiVendorAPI.Repositories.Interfaces;
using MultiVendorAPI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiVendorAPI.Services
{
    public class AddressService : IAddressService
    {
        private readonly IAddressRepository _addressRepository;

        public AddressService(IAddressRepository addressRepository)
        {
            _addressRepository = addressRepository;
        }

        public async Task<ServiceResponse<AddressResponseDto>> GetAddressByIdAsync(long id)
        {
            var address = await _addressRepository.GetByIdAsync(id);
            if (address == null)
            {
                return ServiceResponse<AddressResponseDto>.FailureResponse("Address not found", 404);
            }

            var response = MapToResponseDto(address);
            return ServiceResponse<AddressResponseDto>.SuccessResponse(response, "Address retrieved successfully");
        }

        public async Task<ServiceResponse<List<AddressResponseDto>>> GetAddressesByUserIdAsync(long userId)
        {
            var addresses = await _addressRepository.GetByUserIdAsync(userId);
            var responseList = addresses.Select(MapToResponseDto).ToList();
            return ServiceResponse<List<AddressResponseDto>>.SuccessResponse(responseList, "Addresses retrieved successfully");
        }

        public async Task<ServiceResponse<AddressResponseDto>> CreateAddressAsync(CreateAddressDto dto)
        {
            var existingAddresses = await _addressRepository.GetByUserIdAsync(dto.UserId);
            bool isFirstAddress = !existingAddresses.Any();

            var address = new Address
            {
                UserId = dto.UserId,
                FullName = dto.FullName,
                Phone = dto.Phone,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                City = dto.City,
                State = dto.State,
                Country = dto.Country ?? "India",
                PostalCode = dto.PostalCode,
                AddressType = dto.AddressType,
                IsDefault = isFirstAddress || dto.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            if (address.IsDefault && !isFirstAddress)
            {
                await _addressRepository.ResetDefaultAddressesAsync(dto.UserId);
            }

            await _addressRepository.AddAsync(address);
            await _addressRepository.SaveChangesAsync();

            var response = MapToResponseDto(address);
            return ServiceResponse<AddressResponseDto>.SuccessResponse(response, "Address created successfully", 201);
        }

        public async Task<ServiceResponse<AddressResponseDto>> UpdateAddressAsync(long id, UpdateAddressDto dto)
        {
            var address = await _addressRepository.GetByIdAsync(id);
            if (address == null)
            {
                return ServiceResponse<AddressResponseDto>.FailureResponse("Address not found", 404);
            }

            if (dto.IsDefault && !address.IsDefault)
            {
                await _addressRepository.ResetDefaultAddressesAsync(address.UserId);
            }

            address.FullName = dto.FullName;
            address.Phone = dto.Phone;
            address.AddressLine1 = dto.AddressLine1;
            address.AddressLine2 = dto.AddressLine2;
            address.City = dto.City;
            address.State = dto.State;
            address.Country = dto.Country ?? "India";
            address.PostalCode = dto.PostalCode;
            address.AddressType = dto.AddressType;
            address.IsDefault = dto.IsDefault;

            await _addressRepository.UpdateAsync(address);
            await _addressRepository.SaveChangesAsync();

            var response = MapToResponseDto(address);
            return ServiceResponse<AddressResponseDto>.SuccessResponse(response, "Address updated successfully");
        }

        public async Task<ServiceResponse<bool>> DeleteAddressAsync(long id)
        {
            var address = await _addressRepository.GetByIdAsync(id);
            if (address == null)
            {
                return ServiceResponse<bool>.FailureResponse("Address not found", 404);
            }

            bool wasDefault = address.IsDefault;
            long? userId = address.UserId;

            await _addressRepository.DeleteAsync(address);
            await _addressRepository.SaveChangesAsync();

            // If we deleted the default address, make another address default if one exists
            if (wasDefault)
            {
                var remainingAddresses = await _addressRepository.GetByUserIdAsync(userId);
                if (remainingAddresses.Any())
                {
                    var newDefault = remainingAddresses.First();
                    newDefault.IsDefault = true;
                    await _addressRepository.UpdateAsync(newDefault);
                    await _addressRepository.SaveChangesAsync();
                }
            }

            return ServiceResponse<bool>.SuccessResponse(true, "Address deleted successfully");
        }

        private static AddressResponseDto MapToResponseDto(Address address)
        {
            return new AddressResponseDto
            {
                Id = address.Id,
                UserId = address.UserId,
                FullName = address.FullName,
                Phone = address.Phone,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                Country = address.Country,
                PostalCode = address.PostalCode,
                AddressType = address.AddressType,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt
            };
        }
    }
}
