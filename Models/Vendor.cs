using System.ComponentModel.DataAnnotations.Schema;

[Table("Vendors")]
public class Vendor
{
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long? UserId { get; set; }

    [Column("shop_name")]
    public string? ShopName { get; set; }

    [Column("shop_slug")]
    public string? ShopSlug { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("logo")]
    public string? Logo { get; set; }

    [Column("banner")]
    public string? Banner { get; set; }

    [Column("gst_number")]
    public string? GstNumber { get; set; }

    [Column("commission_rate")]
    public decimal? CommissionRate { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}