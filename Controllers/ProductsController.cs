using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MultiVendorAPI.Services.Interfaces;
using MultiVendorAPI.DTOs;
using MultiVendorAPI.Models;
using System.Security.Claims;

namespace MultiVendorAPI.Controllers
{
    [ApiController]
    [Route("products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _productService.GetProductsAsync();

            return Ok(products);
        }



        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetProductById(long id)
        {
            var response = await _productService.GetProductByIdAsync(id);

            return StatusCode(
                response.StatusCode,
                response);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateProductById(long id, UpdateProductDto dto)
        {
            var result = await _productService.UpdateProductAsync(id, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpPost]
        [Authorize(Policy = "VendorOnly")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            // Read the vendor's own Id from the "vendorId" claim set during vendor login
            var vendorIdClaim = User.FindFirstValue("vendorId");

            if (string.IsNullOrEmpty(vendorIdClaim) || !int.TryParse(vendorIdClaim, out var vendorId))
                return Unauthorized(new { message = "Vendor identity could not be determined from token." });

            // Stamp VendorId onto the DTO (not from request body)
            dto.VendorId = vendorId;

            var result = await _productService.CreateProductAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteProduct(long id)
        {
            var response = await _productService.DeleteProductAsync(id);

            return StatusCode(
                response.StatusCode,
                response);
        }

        [HttpPut("{id:long}/block")]
        public async Task<IActionResult> BlockProduct(long id)
        {
            var response = await _productService.BlockProductByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("blocked")]
        public async Task<IActionResult> GetBlockedProducts()
        {
            var response = await _productService.GetBlockedProductsAsync();
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var response = await _productService.GetCategoriesAsync();

            return StatusCode(
                response.StatusCode,
                response);
        }

        [HttpGet("search/{searchTerm}")]
        public async Task<IActionResult> SearchProducts(string searchTerm)
        {
            var response = await _productService.SearchProductsAsync(searchTerm);

            return StatusCode(
                response.StatusCode,
                response);
        }

    }
}