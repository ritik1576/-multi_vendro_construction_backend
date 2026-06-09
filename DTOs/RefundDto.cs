public class RefundDto
{
    public string RazorpayPaymentId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}