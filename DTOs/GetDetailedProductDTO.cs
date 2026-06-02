public class GetDetailedProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string Thumbnail { get; set; } = string.Empty;
    public long? CategoryId { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public List<string> Images { get; set; } = new List<string>();
    public string Category { get; set; } = string.Empty;

    public string VendorName { get; set; } = string.Empty;
}
