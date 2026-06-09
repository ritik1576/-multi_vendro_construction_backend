using Microsoft.AspNetCore.Mvc;
using MultiVendorAPI.DTOs.AddressDTOs;
using MultiVendorAPI.Services.Interfaces;
using System.Threading.Tasks;

namespace MultiVendorAPI.Controllers
{
    [ApiController]
    [Route("addresses")]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _addressService.CreateAddressAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetAddress(long id)
        {
            var response = await _addressService.GetAddressByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("user/{userId:long}")]
        public async Task<IActionResult> GetUserAddresses(long userId)
        {
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

            var response = await _addressService.UpdateAddressAsync(id, dto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteAddress(long id)
        {
            var response = await _addressService.DeleteAddressAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}
