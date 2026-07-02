using System;

namespace InframartAPI_New.DTOs
{
    public class WalletTransactionResponseDto
    {
        public long Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
