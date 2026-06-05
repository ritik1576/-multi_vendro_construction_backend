public class OrderListDto
{
    public long Id { get; set; }

    public string? OrderNumber { get; set; }

    public string VendorName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string? OrderStatus { get; set; }

    public string DisplayStatus { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }

    public int ItemCount { get; set; }
}
