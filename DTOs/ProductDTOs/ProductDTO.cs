namespace MultiVendorAPI.DTOs
{
    public class ProductDto
    {
        public long Id { get; set; }

        public string? Name { get; set; }

        public decimal? Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public string? Thumbnail { get; set; }

        public long? CategoryId { get; set; }

        public string? ShortDescription { get; set; }

        public string? Unit { get; set; }

        public string? Category { get; set; }
    }
}