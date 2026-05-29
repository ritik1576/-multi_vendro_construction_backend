using MultiVendorAPI.DTOs;
using MultiVendorAPI.Common;

namespace MultiVendorAPI.Services.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetProductsAsync();
        Task<ServiceResponse<ProductDto>> CreateProductAsync(CreateProductDto dto);
        Task<ServiceResponse<ProductDto>> GetProductByNameAsync(string name);
        Task<ServiceResponse<ProductDto>> UpdateProductAsync(
    string name,
    UpdateProductDto dto);
        Task<ServiceResponse<string>> DeleteProductAsync(string productName);
    }
}