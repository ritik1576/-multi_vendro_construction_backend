using InframartAPI_New.Models;
using System.Threading.Tasks;

namespace InframartAPI_New.Repositories.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetWalletByUserIdAsync(long userId);
        Task<string?> GetUserNameAsync(long userId);
        Task<string?> GetVendorNameByUserIdAsync(long userId);
        Task<decimal> GetMonthlyExpenditureAsync(long walletId);
        Task<(System.Collections.Generic.List<WalletTransaction> items, int totalCount)> GetWalletTransactionsAsync(long walletId, int page, int pageSize);
        Task UpdateWalletAsync(Wallet wallet);
        Task AddTransactionAsync(WalletTransaction transaction);
        Task SaveChangesAsync();
    }
}
