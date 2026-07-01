using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Data;
using MultiVendorAPI.DTOs;
using MultiVendorAPI.Services.Interfaces;
using MultiVendorAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using MultiVendorAPI.Common;
using InframartAPI_New.Services.Interfaces;

namespace MultiVendorAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileUploadService _fileUploadService;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private const string CategoriesCacheKey = "categories_list";

        public ProductService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IFileUploadService fileUploadService,
            Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _fileUploadService = fileUploadService;
            _cache = cache;
        }

        private string? FormatThumbnailUrl(string? thumbnail)
        {
            if (string.IsNullOrEmpty(thumbnail))
                return thumbnail;

            if (thumbnail.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                thumbnail.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return thumbnail;
            }

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
            {
                return $"/sys/stream/{thumbnail}";
            }

            var scheme = request.Scheme;
            var host = request.Host;
            return $"{scheme}://{host}/sys/stream/{thumbnail}";
        }

        public async Task<List<ProductDto>> GetProductsAsync()
        {
            var products = await _context.Products
                .Where(p => p.Status != "inactive" && p.Status != "deleted")
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    ProductId = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    Thumbnail = p.Thumbnail,
                    ThumbnailUrl = p.ThumbnailImageUrl ?? p.Thumbnail,
                    AverageRating = _context.Reviews.Where(r => r.ProductId == p.Id).Average(r => (double?)r.Rating) ?? 0.0,
                    ReviewCount = _context.Reviews.Count(r => r.ProductId == p.Id),
                    CategoryId = p.CategoryId,
                    ShortDescription = p.ShortDescription,
                    Unit = p.Unit,
                    Category = _context.Categories
                        .Where(c => c.Id == p.CategoryId)
                        .Select(c => c.Name)
                        .FirstOrDefault()
                })
                .ToListAsync();

            foreach (var prod in products)
            {
                prod.Thumbnail = FormatThumbnailUrl(prod.Thumbnail);
                prod.ThumbnailUrl = FormatThumbnailUrl(prod.ThumbnailUrl);
                prod.Images = new List<string>();
            }

            return products;
        }



        public async Task<ServiceResponse<ProductDto>> CreateProductAsync(
            CreateProductDto dto)

        {
            try
            {
                if (!Validations.ValidateProductDto(dto))
                {
                    return ServiceResponse<ProductDto>
                        .FailureResponse("Invalid product data", 400);
                }
            }
            catch (Exception ex)
            {
                return ServiceResponse<ProductDto>.FailureResponse(ex.Message, 400);
            }

            var category = await _context.Categories
        .FirstOrDefaultAsync(c => c.Name == dto.Category);

            if (category == null)
            {
                return ServiceResponse<ProductDto>.FailureResponse(
                    "Category not found",
                    404);
            }


            var uploadedResults = new List<InframartAPI_New.DTOs.ProductImageUploadResult>();
            var uploadedUrls = new List<string>();
            if (dto.Images != null && dto.Images.Any())
            {
                try
                {
                    foreach (var file in dto.Images)
                    {
                        var uploadRes = await _fileUploadService.UploadProductImageAsync(file);
                        uploadedResults.Add(uploadRes);
                        uploadedUrls.Add(uploadRes.OriginalUrl);
                    }
                }
                catch (Exception ex)
                {
                    return ServiceResponse<ProductDto>.FailureResponse($"Image upload failed: {ex.Message}", 500);
                }
            }

            var primaryResult = uploadedResults.FirstOrDefault();
            var product = new Product
            {
                VendorId = dto.VendorId,
                CategoryId = category.Id,
                Name = dto.Name,
                Slug = dto.Slug,
                ShortDescription = dto.ShortDescription,
                Description = dto.Description,
                Price = dto.Price,
                DiscountPrice = dto.DiscountPrice,
                Sku = dto.Sku,
                Thumbnail = string.IsNullOrEmpty(dto.Thumbnail) && uploadedUrls.Any() ? uploadedUrls.First() : dto.Thumbnail,
                OriginalImageUrl = primaryResult != null ? primaryResult.OriginalUrl : dto.Thumbnail,
                ThumbnailImageUrl = primaryResult != null ? primaryResult.ThumbnailUrl : dto.Thumbnail,
                Images = uploadedUrls,
                InStock = dto.InStock,
                Quantity = dto.Quantity,
                Unit = dto.Unit,
                CreatedAt = DateTime.Now
            };

            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            var productDto = new ProductDto
            {
                Id = product.Id,
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                Thumbnail = FormatThumbnailUrl(product.Thumbnail),
                ThumbnailUrl = FormatThumbnailUrl(product.ThumbnailImageUrl ?? product.Thumbnail),
                Images = product.Images.Select(img => FormatThumbnailUrl(img)!).ToList(),
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

        public async Task<ServiceResponse<GetDetailedProductDto>> GetProductByIdAsync(long id)
        {
            if (id <= 0)
            {
                return ServiceResponse<GetDetailedProductDto>
                    .FailureResponse("Product ID must be greater than zero", 400);
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.Status != "inactive" &&
                    p.Status != "deleted");

            if (product == null)
            {
                return ServiceResponse<GetDetailedProductDto>
                    .FailureResponse(
                        "Product not found",
                        404);
            }

            var categoryName = product.CategoryId.HasValue
                ? await _context.Categories
                    .AsNoTracking()
                    .Where(c => c.Id == product.CategoryId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync()
                : null;

            var vendorName = product.VendorId.HasValue
                ? await _context.Vendors
                    .AsNoTracking()
                    .Where(v => v.Id == product.VendorId.Value)
                    .Select(v => v.ShopName)
                    .FirstOrDefaultAsync()
                : null;

            var formattedThumbnail = FormatThumbnailUrl(product.Thumbnail);
            var getDetailedProductDto = new GetDetailedProductDto
            {
                Id = product.Id,
                VendorId = product.VendorId,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Slug = product.Slug,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                LongDescription = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                Sku = product.Sku,
                Thumbnail = formattedThumbnail,
                FullImageUrl = FormatThumbnailUrl(product.OriginalImageUrl ?? product.Thumbnail),
                ThumbnailUrl = FormatThumbnailUrl(product.ThumbnailImageUrl ?? product.Thumbnail),
                Status = product.Status,
                InStock = product.InStock,
                Quantity = product.Quantity,
                Unit = product.Unit,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                Images = product.Images != null && product.Images.Any()
                    ? product.Images.Select(img => FormatThumbnailUrl(img)!).ToList()
                    : (string.IsNullOrWhiteSpace(formattedThumbnail) ? new List<string>() : new List<string> { formattedThumbnail }),
                Category = categoryName,
                VendorName = vendorName
            };

            return ServiceResponse<GetDetailedProductDto>
                .SuccessResponse(
                    getDetailedProductDto,
                    "Product retrieved successfully",
                    200);

        }

        public async Task<ServiceResponse<ProductDto>> UpdateProductAsync(
            long id,
            UpdateProductDto dto,
            long? vendorId,
            string userRole)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return ServiceResponse<ProductDto>
                    .FailureResponse(
                        "Product not found",
                        404);
            }

            if (userRole != "admin" && product.VendorId != vendorId)
            {
                return ServiceResponse<ProductDto>
                    .FailureResponse(
                        "Access denied to update this product",
                        403);
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

            try
            {
                if (!Validations.ValidateProductDto(dto))
                {
                    return ServiceResponse<ProductDto>
                       .FailureResponse("Invalid product data", 400);
                }
            }
            catch (Exception ex)
            {
                return ServiceResponse<ProductDto>.FailureResponse(ex.Message, 400);
            }

            product.Name = dto.Name;

            product.Slug = dto.Slug;

            product.ShortDescription = dto.ShortDescription;

            product.Description = dto.Description;

            product.Price = dto.Price;

            product.DiscountPrice = dto.DiscountPrice;

            product.Sku = dto.Sku;

            product.Thumbnail = dto.Thumbnail;
            if (!string.IsNullOrEmpty(dto.Thumbnail))
            {
                product.OriginalImageUrl = dto.Thumbnail;
                product.ThumbnailImageUrl = dto.Thumbnail;
            }

            if (dto.Images != null && dto.Images.Any())
            {
                try
                {
                    var uploadedResults = new List<InframartAPI_New.DTOs.ProductImageUploadResult>();
                    var uploadedUrls = new List<string>();
                    foreach (var file in dto.Images)
                    {
                        var uploadRes = await _fileUploadService.UploadProductImageAsync(file);
                        uploadedResults.Add(uploadRes);
                        uploadedUrls.Add(uploadRes.OriginalUrl);
                    }
                    product.Images = uploadedUrls;
                    var primaryResult = uploadedResults.FirstOrDefault();
                    if (primaryResult != null)
                    {
                        product.OriginalImageUrl = primaryResult.OriginalUrl;
                        product.ThumbnailImageUrl = primaryResult.ThumbnailUrl;
                        if (string.IsNullOrEmpty(product.Thumbnail))
                        {
                            product.Thumbnail = primaryResult.OriginalUrl;
                        }
                    }
                }
                catch (Exception ex)
                {
                    return ServiceResponse<ProductDto>.FailureResponse($"Image upload failed: {ex.Message}", 500);
                }
            }

            product.Status = dto.Status;

            product.InStock = dto.InStock;

            product.Quantity = dto.Quantity;

            product.Unit = dto.Unit;

            // updated_at
            product.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return ServiceResponse<ProductDto>
                .SuccessResponse(
                    new ProductDto
                    {
                        Id = product.Id,
                        ProductId = product.Id,
                        Name = product.Name,
                        Price = product.Price,
                        DiscountPrice = product.DiscountPrice,
                        Thumbnail = FormatThumbnailUrl(product.Thumbnail),
                        ThumbnailUrl = FormatThumbnailUrl(product.ThumbnailImageUrl ?? product.Thumbnail),
                        Images = product.Images.Select(img => FormatThumbnailUrl(img)!).ToList(),
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
            long id,
            long? vendorId,
            string userRole)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return ServiceResponse<string>
                    .FailureResponse(
                        "Product not found",
                        404);
            }

            if (userRole != "admin" && product.VendorId != vendorId)
            {
                return ServiceResponse<string>
                    .FailureResponse(
                        "Access denied to delete this product",
                        403);
            }

            product.Status = "deleted";
            product.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

#pragma warning disable CS8604 // Possible null reference argument.
            return ServiceResponse<string>
                .SuccessResponse(
                    product.Name,
                    "Product deleted successfully",
                    200);
#pragma warning restore CS8604 // Possible null reference argument.
        }
        public async Task<ServiceResponse<List<string>>> GetCategoriesAsync()
        {
            if (!_cache.TryGetValue(CategoriesCacheKey, out List<string>? categories))
            {
                Console.WriteLine("[Cache Miss] Fetching categories from database.");
                categories = await _context.Categories
                    .Select(c => c.Name ?? string.Empty)
                    .ToListAsync();

                _cache.Set(CategoriesCacheKey, categories, new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                });
            }
            else
            {
                Console.WriteLine("[Cache Hit] Served categories from memory cache.");
            }

            return ServiceResponse<List<string>>
                .SuccessResponse(
                    categories!,
                    "Categories retrieved successfully",
                    200);
        }
        public async Task<ServiceResponse<List<ProductDto>>> SearchProductsAsync(string searchTerm)
        {
            var products = await _context.Products
                .Where(p => p.Name != null && p.Name.Contains(searchTerm) && p.Status != "inactive" && p.Status != "deleted")
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    ProductId = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    Thumbnail = p.Thumbnail,
                    ThumbnailUrl = p.ThumbnailImageUrl ?? p.Thumbnail,
                    AverageRating = _context.Reviews.Where(r => r.ProductId == p.Id).Average(r => (double?)r.Rating) ?? 0.0,
                    ReviewCount = _context.Reviews.Count(r => r.ProductId == p.Id),
                    CategoryId = p.CategoryId,
                    ShortDescription = p.ShortDescription,
                    Unit = p.Unit,
                    Category = _context.Categories
                        .Where(c => c.Id == p.CategoryId)
                        .Select(c => c.Name)
                        .FirstOrDefault()
                })
                .ToListAsync();

            foreach (var prod in products)
            {
                prod.Thumbnail = FormatThumbnailUrl(prod.Thumbnail);
                prod.ThumbnailUrl = FormatThumbnailUrl(prod.ThumbnailUrl);
                prod.Images = new List<string>();
            }

            return ServiceResponse<List<ProductDto>>
                .SuccessResponse(
                    products,
                    "Products retrieved successfully",
                    200);
        }

        public async Task<ServiceResponse<bool>> BlockProductByIdAsync(long id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return ServiceResponse<bool>.FailureResponse("Product not found", 404);
            }

            product.Status = "inactive";
            product.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return ServiceResponse<bool>.SuccessResponse(true, "Product blocked successfully", 200);
        }

        public async Task<ServiceResponse<List<ProductDto>>> GetBlockedProductsAsync()
        {
            var products = await _context.Products
                .Where(p => p.Status == "inactive")
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    ProductId = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    Thumbnail = p.Thumbnail,
                    ThumbnailUrl = p.ThumbnailImageUrl ?? p.Thumbnail,
                    AverageRating = _context.Reviews.Where(r => r.ProductId == p.Id).Average(r => (double?)r.Rating) ?? 0.0,
                    ReviewCount = _context.Reviews.Count(r => r.ProductId == p.Id),
                    CategoryId = p.CategoryId,
                    ShortDescription = p.ShortDescription,
                    Unit = p.Unit,
                    Category = _context.Categories
                        .Where(c => c.Id == p.CategoryId)
                        .Select(c => c.Name)
                        .FirstOrDefault()
                })
                .ToListAsync();

            foreach (var prod in products)
            {
                prod.Thumbnail = FormatThumbnailUrl(prod.Thumbnail);
                prod.ThumbnailUrl = FormatThumbnailUrl(prod.ThumbnailUrl);
                prod.Images = new List<string>();
            }

            return ServiceResponse<List<ProductDto>>.SuccessResponse(products, "Blocked products retrieved successfully", 200);
        }

        public async Task<ServiceResponse<Category>> CreateCategoryAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return ServiceResponse<Category>.FailureResponse("Category name cannot be empty", 400);
            }

            var category = new Category
            {
                Name = name,
                Slug = name.ToLower().Replace(" ", "-"),
                Status = "active"
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            // Invalidate cache and reload
            _cache.Remove(CategoriesCacheKey);
            Console.WriteLine("[Cache Invalidate] Invalidated category cache due to category creation.");
            await GetCategoriesAsync();

            return ServiceResponse<Category>.SuccessResponse(category, "Category created successfully", 201);
        }

        public async Task<ServiceResponse<Category>> UpdateCategoryAsync(long id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return ServiceResponse<Category>.FailureResponse("Category name cannot be empty", 400);
            }

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return ServiceResponse<Category>.FailureResponse("Category not found", 404);
            }

            category.Name = name;
            category.Slug = name.ToLower().Replace(" ", "-");

            await _context.SaveChangesAsync();

            // Invalidate cache and reload
            _cache.Remove(CategoriesCacheKey);
            Console.WriteLine("[Cache Invalidate] Invalidated category cache due to category update.");
            await GetCategoriesAsync();

            return ServiceResponse<Category>.SuccessResponse(category, "Category updated successfully", 200);
        }

        public async Task<ServiceResponse<bool>> DeleteCategoryAsync(long id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return ServiceResponse<bool>.FailureResponse("Category not found", 404);
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            // Invalidate cache and reload
            _cache.Remove(CategoriesCacheKey);
            Console.WriteLine("[Cache Invalidate] Invalidated category cache due to category deletion.");
            await GetCategoriesAsync();

            return ServiceResponse<bool>.SuccessResponse(true, "Category deleted successfully", 200);
        }
    }
}
