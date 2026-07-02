namespace MultiVendorAPI.DTOs
{
    public class ProductByNameDTO
    {

        public long VendorId { get; set; }

        public long CategoryId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public string? Sku { get; set; }

        public string? Thumbnail { get; set; }

        public bool InStock { get; set; }

        public int Quantity { get; set; }

    }
}