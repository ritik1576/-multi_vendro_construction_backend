using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MultiVendorAPI.Services.Interfaces;
using MultiVendorAPI.DTOs;
using MultiVendorAPI.Models;
using System.Security.Claims;
using InframartAPI_New.Middlewares;

namespace MultiVendorAPI.Controllers
{
    [ApiController]
    [Route("products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService _productService)
        {
            this._productService = _productService;
        }

        private (long? vendorId, string role) GetCurrentUser()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var vendorIdClaim = User.FindFirst("vendorId")?.Value;
            long? vendorId = null;
            if (!string.IsNullOrEmpty(vendorIdClaim) && long.TryParse(vendorIdClaim, out var vId))
            {
                vendorId = vId;
            }
            return (vendorId, role);
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
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "vendor,admin")]
        public async Task<IActionResult> UpdateProductById(long id, [FromBody] UpdateProductDto dto)
        {
            var (vendorId, role) = GetCurrentUser();
            var result = await _productService.UpdateProductAsync(id, dto, vendorId, role);

            if (!result.Success)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "vendor")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            var (vendorId, _) = GetCurrentUser();
            if (vendorId == null)
                throw new ForbiddenException("Vendor identity could not be determined from token.");

            dto.VendorId = (int)vendorId.Value;

            var result = await _productService.CreateProductAsync(dto);

            if (!result.Success)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "vendor,admin")]
        public async Task<IActionResult> DeleteProduct(long id)
        {
            var (vendorId, role) = GetCurrentUser();
            var response = await _productService.DeleteProductAsync(id, vendorId, role);

            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id:long}/block")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> BlockProduct(long id)
        {
            var response = await _productService.BlockProductByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("blocked")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetBlockedProducts()
        {
            var response = await _productService.GetBlockedProductsAsync();
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var response = await _productService.GetCategoriesAsync();
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("search/{searchTerm}")]
        public async Task<IActionResult> SearchProducts(string searchTerm)
        {
            var response = await _productService.SearchProductsAsync(searchTerm);
            return StatusCode(response.StatusCode, response);
        }
    }
}