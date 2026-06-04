namespace MultiVendorAPI.DTOs
{
    public class GetDetailedProductDto
    {
        public long Id { get; set; }

        public long? VendorId { get; set; }

        public long? CategoryId { get; set; }

        public string? Name { get; set; }

        public string? Slug { get; set; }

        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        public string? LongDescription { get; set; }

        public decimal? Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public string? Sku { get; set; }

        public string? Thumbnail { get; set; }

        public string? Status { get; set; }

        public bool? InStock { get; set; }

        public int? Quantity { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<string> Images { get; set; } = new();

        public string? Category { get; set; }

        public string? VendorName { get; set; }
    }
}
