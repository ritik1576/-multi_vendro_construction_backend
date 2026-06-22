using InframartAPI_New.DTOs;
using System.Threading.Tasks;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IWalletService
    {
        Task<WalletBalanceResponseDto?> GetWalletBalanceAsync(long userId);
        Task<(System.Collections.Generic.List<WalletTransactionResponseDto> items, int totalCount)?> GetWalletTransactionsAsync(long userId, int page, int pageSize);
    }
}
