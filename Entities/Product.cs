

namespace MultiVendorAPI.Models
{
    public class Product
    {
        public long Id { get; set; }

        public long? VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public long? CategoryId { get; set; }
        public Category? Category { get; set; }

        public string? Name { get; set; }

        public string? Slug { get; set; }

        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        public decimal? Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public string? Sku { get; set; }

        public string? Thumbnail { get; set; }

        public string? Status { get; set; }

        public bool? InStock { get; set; }

        public int? Quantity { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}