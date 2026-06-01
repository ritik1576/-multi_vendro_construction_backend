using Microsoft.AspNetCore.Mvc;
using MultiVendorAPI.Services.Interfaces;
using MultiVendorAPI.DTOs;
using MultiVendorAPI.Models;

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



        [HttpGet]

        [Route("{name}")]
        public async Task<IActionResult> GetProductByName(string name)
        {
            var response =
            await _productService.GetProductByNameAsync(name);

            return StatusCode(
                response.StatusCode,
                response);
        }

        [HttpPut]

        [Route("{name}")]
        public async Task<IActionResult> UpdateProductByName(string name, UpdateProductDto dto)
        {
            var result = await _productService.UpdateProductAsync(name, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpPost]

        public async Task<IActionResult> CreateProduct(CreateProductDto dto)
        {
            var result = await _productService.CreateProductAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteProduct(
    string name)
        {
            var response =
                await _productService.DeleteProductAsync(name);

            return StatusCode(
                response.StatusCode,
                response);
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