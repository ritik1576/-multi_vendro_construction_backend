public class UpdateProductDto
{
    public string? Name { get; set; }

    public string? CategoryName { get; set; }

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
}