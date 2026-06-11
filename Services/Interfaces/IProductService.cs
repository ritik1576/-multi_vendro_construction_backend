using MultiVendorAPI.DTOs;
using MultiVendorAPI.Common;

namespace MultiVendorAPI.Services.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetProductsAsync();
        Task<ServiceResponse<ProductDto>> CreateProductAsync(CreateProductDto dto);
        Task<ServiceResponse<GetDetailedProductDto>> GetProductByIdAsync(long id);
        Task<ServiceResponse<ProductDto>> UpdateProductAsync(
    long id,
    UpdateProductDto dto);
        Task<ServiceResponse<string>> DeleteProductAsync(long id);
        Task<ServiceResponse<List<string>>> GetCategoriesAsync();
        Task<ServiceResponse<List<ProductDto>>> SearchProductsAsync(string searchTerm);
        Task<ServiceResponse<bool>> BlockProductByIdAsync(long id);
        Task<ServiceResponse<List<ProductDto>>> GetBlockedProductsAsync();
    }
}