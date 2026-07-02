using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorAPI.DTOs.AddressDTOs;
using MultiVendorAPI.Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using InframartAPI_New.Middlewares;

namespace MultiVendorAPI.Controllers
{
    [ApiController]
    [Route("addresses")]
    [Authorize(Roles = "customer")]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !long.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException();
            return userId;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.UserId != GetCurrentUserId())
            {
                throw new ForbiddenException("Cannot create address for another user.");
            }

            var response = await _addressService.CreateAddressAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetAddress(long id)
        {
            var response = await _addressService.GetAddressByIdAsync(id);
            if (response.Success && response.Data != null && response.Data.UserId != GetCurrentUserId())
            {
                throw new ForbiddenException("Access denied to this address.");
            }
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("user/{userId:long}")]
        public async Task<IActionResult> GetUserAddresses(long userId)
        {
            if (userId != GetCurrentUserId())
            {
                throw new ForbiddenException("Access denied to this user's addresses.");
            }
            var response = await _addressService.GetAddressesByUserIdAsync(userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateAddress(long id, [FromBody] UpdateAddressDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _addressService.GetAddressByIdAsync(id);
            if (!existing.Success)
            {
                return StatusCode(existing.StatusCode, existing);
            }

            if (existing.Data != null && existing.Data.UserId != GetCurrentUserId())
            {
                throw new ForbiddenException("Access denied to update this address.");
            }

            var response = await _addressService.UpdateAddressAsync(id, dto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteAddress(long id)
        {
            var existing = await _addressService.GetAddressByIdAsync(id);
            if (!existing.Success)
            {
                return StatusCode(existing.StatusCode, existing);
            }

            if (existing.Data != null && existing.Data.UserId != GetCurrentUserId())
            {
                throw new ForbiddenException("Access denied to delete this address.");
            }

            var response = await _addressService.DeleteAddressAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}
