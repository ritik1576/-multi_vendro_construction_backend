using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InframartAPI_New.Models
{
    [Table("wallets")]
    public class Wallet
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [Column("wallet_type")]
        public WalletType WalletType { get; set; }

        [Column("available_balance")]
        public decimal AvailableBalance { get; set; }

        [Column("locked_balance")]
        public decimal LockedBalance { get; set; }

        [Column("total_credits")]
        public decimal TotalCredits { get; set; }

        [Column("total_debits")]
        public decimal TotalDebits { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Active";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("row_version")]
        public int RowVersion { get; set; } = 1;
    }
}
