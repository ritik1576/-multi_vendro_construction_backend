using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InframartAPI_New.Models
{
    [Table("wallet_transactions")]
    public class WalletTransaction
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("transaction_id")]
        public string TransactionId { get; set; } = string.Empty;

        [Column("wallet_id")]
        public long WalletId { get; set; }

        [Column("transaction_type")]
        public TransactionType TransactionType { get; set; }

        [Column("direction")]
        public TransactionDirection Direction { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("balance_before")]
        public decimal BalanceBefore { get; set; }

        [Column("balance_after")]
        public decimal BalanceAfter { get; set; }

        [Column("available_before")]
        public decimal AvailableBefore { get; set; }

        [Column("available_after")]
        public decimal AvailableAfter { get; set; }

        [Column("locked_before")]
        public decimal LockedBefore { get; set; }

        [Column("locked_after")]
        public decimal LockedAfter { get; set; }

        [Column("reference_type")]
        public string? ReferenceType { get; set; }

        [Column("reference_id")]
        public string? ReferenceId { get; set; }

        [Column("parent_transaction_id")]
        public long? ParentTransactionId { get; set; }

        [Column("idempotency_key")]
        public string? IdempotencyKey { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("status")]
        public TransactionStatus Status { get; set; }

        [Column("created_by")]
        public string CreatedBy { get; set; } = "System";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
