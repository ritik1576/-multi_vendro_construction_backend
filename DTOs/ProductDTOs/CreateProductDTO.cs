namespace MultiVendorAPI.DTOs
{
    public class CreateProductDto
    {
        // Injected from JWT by the controller — NOT sent in the request body
        public int? VendorId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public string? Sku { get; set; }

        public string? Thumbnail { get; set; }

        public bool InStock { get; set; }

        public DateTime CreatedAt { get; set; }

        public int Quantity { get; set; }

        public string? Unit { get; set; }

        public string? Category { get; set; }
    }
}