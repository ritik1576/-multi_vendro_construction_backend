using InframartAPI_New.DTOs;
using System.Threading.Tasks;
using MultiVendorAPI.Common;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IWalletService
    {
        Task<WalletBalanceResponseDto?> GetWalletBalanceAsync(long userId);
        Task<(System.Collections.Generic.List<WalletTransactionResponseDto> items, int totalCount)?> GetWalletTransactionsAsync(long userId, int page, int pageSize);
        Task<(bool success, string message, WalletBalanceResponseDto? wallet)> AddMoneyAsync(long userId, AddMoneyRequestDto dto);
        Task<(bool success, string message, WalletBalanceResponseDto? wallet)> WithdrawMoneyAsync(long userId, WithdrawMoneyRequestDto dto);
        Task<(bool success, string message, WalletBalanceResponseDto? wallet)> TransferMoneyAsync(long userId, TransferMoneyRequestDto dto);
        Task<ServiceResponse<object>> CreateAddMoneyPaymentAsync(long userId, CreateWalletRechargeDto dto);
        Task<ServiceResponse<object>> VerifyAddMoneyAsync(long userId, VerifyWalletRechargeDto dto);
    }
}
