public class OrderListDto
{
    public long Id { get; set; }

    public string? OrderNumber { get; set; }

    public decimal TotalAmount { get; set; }

    public string? OrderStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int ItemCount { get; set; }
}