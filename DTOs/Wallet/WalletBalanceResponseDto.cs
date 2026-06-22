namespace InframartAPI_New.DTOs
{
    public class WalletBalanceResponseDto
    {
        public long WalletId { get; set; }
        public string WalletType { get; set; } = string.Empty;
        public decimal AvailableBalance { get; set; }
        public decimal LockedBalance { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal TotalDebits { get; set; }
        public string Status { get; set; } = "Active";
    }
}
