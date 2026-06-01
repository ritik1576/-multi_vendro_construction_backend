using MultiVendorAPI.DTOs;

public class Validations
{
    public static bool ValidateProductDto(CreateProductDto product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (string.IsNullOrWhiteSpace(product.Name))
            throw new Exception("Product name is required.");

        if (string.IsNullOrWhiteSpace(product.Slug))
            throw new Exception("Product slug is required.");

        if (product.Price <= 0)
            throw new Exception("Price must be greater than 0.");

        if (product.DiscountPrice.HasValue &&
            product.DiscountPrice > product.Price)
            throw new Exception("Discount price cannot exceed actual price.");

        if (product.Quantity < 0)
            throw new Exception("Quantity cannot be negative.");

        if (string.IsNullOrWhiteSpace(product.Sku))
            throw new Exception("SKU is required.");

        if (product.Name.Length > 255)
            throw new Exception("Product name cannot exceed 255 characters.");

        if (product.Slug.Length > 255)
            throw new Exception("Product slug cannot exceed 255 characters.");

        return true;
    }
    public static bool ValidateProductDto(UpdateProductDto product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (string.IsNullOrWhiteSpace(product.Name))
            throw new Exception("Product name is required.");

        if (string.IsNullOrWhiteSpace(product.Slug))
            throw new Exception("Product slug is required.");

        if (product.Price <= 0)
            throw new Exception("Price must be greater than 0.");

        if (product.DiscountPrice.HasValue &&
            product.DiscountPrice > product.Price)
            throw new Exception("Discount price cannot exceed actual price.");

        if (product.Quantity < 0)
            throw new Exception("Quantity cannot be negative.");

        if (string.IsNullOrWhiteSpace(product.Sku))
            throw new Exception("SKU is required.");

        if (product.Name.Length > 255)
            throw new Exception("Product name cannot exceed 255 characters.");

        if (product.Slug.Length > 255)
            throw new Exception("Product slug cannot exceed 255 characters.");

        return true;
    }

}