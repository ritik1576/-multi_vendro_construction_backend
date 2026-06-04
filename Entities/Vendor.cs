public class Vendor
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string shop_name { get; set; } = string.Empty;

    public string ShopSlug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Logo { get; set; } = string.Empty;

    public string Banner { get; set; } = string.Empty;

    public string GstNumber { get; set; } = string.Empty;

    public decimal? CommissionRate { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
