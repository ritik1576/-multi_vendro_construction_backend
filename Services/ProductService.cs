using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Data;
using MultiVendorAPI.DTOs;
using MultiVendorAPI.Services.Interfaces;
using MultiVendorAPI.Models;
using MultiVendorAPI.Common;

namespace MultiVendorAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProductDto>> GetProductsAsync()
        {


            var products = await _context.Products
                .Select(p => new ProductDto
                {

                    Name = p.Name,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    Thumbnail = p.Thumbnail,
                    CategoryId = p.CategoryId,
                    ShortDescription = p.ShortDescription,
                    Category = _context.Categories
                        .Where(c => c.Id == p.CategoryId)
                        .Select(c => c.Name)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return products;
        }



        public async Task<ServiceResponse<ProductDto>> CreateProductAsync(
            CreateProductDto dto)

        {
            if (!Validations.ValidateProductDto(dto))
            {
                return ServiceResponse<ProductDto>
                    .FailureResponse("Invalid product data", 400);
            }

            var category = await _context.Categories
        .FirstOrDefaultAsync(c => c.Name == dto.Category);

            if (category == null)
            {
                return ServiceResponse<ProductDto>.FailureResponse(
                    "Category not found",
                    404);
            }


            var product = new Product
            {
                CategoryId = category.Id,
                Name = dto.Name,
                Slug = dto.Slug,
                ShortDescription = dto.ShortDescription,
                Description = dto.Description,
                Price = dto.Price,
                DiscountPrice = dto.DiscountPrice,
                Sku = dto.Sku,
                Thumbnail = dto.Thumbnail,
                InStock = dto.InStock,
                Quantity = dto.Quantity,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            var productDto = new ProductDto
            {
                Name = product.Name,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                Thumbnail = product.Thumbnail,
                CategoryId = product.CategoryId,
                ShortDescription = product.ShortDescription,
                Category = category.Name
            };

            return ServiceResponse<ProductDto>
                .SuccessResponse(
                    productDto,
                    "Product created successfully",
                    201);
        }

        public async Task<ServiceResponse<GetDetailedProductDto>> GetProductByNameAsync(string name)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Name != null &&
                    p.Name.ToLower() == name.ToLower());

            if (product == null)
            {
                return ServiceResponse<GetDetailedProductDto>
                    .FailureResponse(
                        "Product not found",
                        404);
            }

            var getDetailedProductDto = new GetDetailedProductDto
            {
                Name = product.Name,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                Thumbnail = product.Thumbnail,
                CategoryId = product.CategoryId,
                ShortDescription = product.ShortDescription,
                LongDescription = product.Description,
                Images = product.Thumbnail != null
                    ? new List<string> { product.Thumbnail }
                    : new List<string>(),

                Category = await _context.Categories
                    .Where(c => c.Id == product.CategoryId)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync(),

                VendorName = await _context.Vendors
                    .Where(v => v.Id == product.VendorId)
                    .Select(v => v.BusinessName)
                    .FirstOrDefaultAsync()
            };

            return ServiceResponse<GetDetailedProductDto>
                .SuccessResponse(
                    getDetailedProductDto,
                    "Product retrieved successfully",
                    200);
        }

        public async Task<ServiceResponse<ProductDto>> UpdateProductAsync(
    string name,
    UpdateProductDto dto)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Name == name);

            if (product == null)
            {
                return ServiceResponse<ProductDto>
                    .FailureResponse(
                        "Product not found",
                        404);
            }

            // Category update
            if (!string.IsNullOrWhiteSpace(dto.CategoryName))
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == dto.CategoryName);

                if (category == null)
                {
                    return ServiceResponse<ProductDto>
                        .FailureResponse(
                            "Category not found",
                            404);
                }

                product.CategoryId = category.Id;
            }

            // Update allowed fields

            if (!Validations.ValidateProductDto(dto))
            {
                return ServiceResponse<ProductDto>
                   .FailureResponse("Invalid product data", 400);
            }

            product.Name = dto.Name;

            product.Slug = dto.Slug;

            product.ShortDescription = dto.ShortDescription;

            product.Description = dto.Description;

            product.Price = dto.Price;

            product.DiscountPrice = dto.DiscountPrice;

            product.Sku = dto.Sku;

            product.Thumbnail = dto.Thumbnail;

            product.Status = dto.Status;

            product.InStock = dto.InStock;

            product.Quantity = dto.Quantity;

            // updated_at
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResponse<ProductDto>
                .SuccessResponse(
                    new ProductDto
                    {
                        Name = product.Name,
                        Price = product.Price,
                        DiscountPrice = product.DiscountPrice,
                        Thumbnail = product.Thumbnail,
                        CategoryId = product.CategoryId,
                        ShortDescription = product.ShortDescription,
                        Category = _context.Categories
                            .Where(c => c.Id == product.CategoryId)
                            .Select(c => c.Name)
                            .FirstOrDefault()
                    },

                    "Product updated successfully",
                    200);
        }

        public async Task<ServiceResponse<string>> DeleteProductAsync(
    string productName)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(
                    p => p.Name.ToLower() == productName.ToLower());

            if (product == null)
            {
                return ServiceResponse<string>
                    .FailureResponse(
                        "Product not found",
                        404);
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            return ServiceResponse<string>
                .SuccessResponse(
                    product.Name,
                    "Product deleted successfully",
                    200);
        }
        public async Task<ServiceResponse<List<string>>> GetCategoriesAsync()
        {
            var categories = await _context.Categories
                .Select(c => c.Name)
                .ToListAsync();

            return ServiceResponse<List<string>>
                .SuccessResponse(
                    categories,
                    "Categories retrieved successfully",
                    200);
        }
        public async Task<ServiceResponse<List<ProductDto>>> SearchProductsAsync(string searchTerm)
        {
            var products = await _context.Products
                .Where(p => p.Name.Contains(searchTerm))
                .Select(p => new ProductDto
                {
                    Name = p.Name,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    Thumbnail = p.Thumbnail,
                    CategoryId = p.CategoryId,
                    ShortDescription = p.ShortDescription,
                    Category = _context.Categories
                        .Where(c => c.Id == p.CategoryId)
                        .Select(c => c.Name)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return ServiceResponse<List<ProductDto>>
                .SuccessResponse(
                    products,
                    "Products retrieved successfully",
                    200);
        }

    }







}
