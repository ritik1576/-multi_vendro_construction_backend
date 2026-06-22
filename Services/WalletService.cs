using InframartAPI_New.DTOs;
using InframartAPI_New.Repositories.Interfaces;
using InframartAPI_New.Services.Interfaces;
using System.Threading.Tasks;

namespace InframartAPI_New.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;

        public WalletService(IWalletRepository walletRepository)
        {
            _walletRepository = walletRepository;
        }

        public async Task<WalletBalanceResponseDto?> GetWalletBalanceAsync(long userId)
        {
            var wallet = await _walletRepository.GetWalletByUserIdAsync(userId);
            if (wallet == null)
            {
                return null;
            }

            var monthlyExpenditure = await _walletRepository.GetMonthlyExpenditureAsync(wallet.Id);

            return new WalletBalanceResponseDto
            {
                WalletId = wallet.Id,
                WalletType = wallet.WalletType.ToString(),
                AvailableBalance = wallet.AvailableBalance,
                LockedBalance = wallet.LockedBalance,
                TotalCredits = wallet.TotalCredits,
                TotalDebits = wallet.TotalDebits,
                MonthlyExpenditure = monthlyExpenditure,
                Status = wallet.Status
            };
        }

        public async Task<(System.Collections.Generic.List<WalletTransactionResponseDto> items, int totalCount)?> GetWalletTransactionsAsync(long userId, int page, int pageSize)
        {
            var wallet = await _walletRepository.GetWalletByUserIdAsync(userId);
            if (wallet == null)
            {
                return null;
            }

            var (transactions, totalCount) = await _walletRepository.GetWalletTransactionsAsync(wallet.Id, page, pageSize);

            var items = new System.Collections.Generic.List<WalletTransactionResponseDto>();
            foreach (var t in transactions)
            {
                items.Add(new WalletTransactionResponseDto
                {
                    Id = t.Id,
                    TransactionId = t.TransactionId,
                    Amount = t.Amount,
                    TransactionType = t.TransactionType.ToString(),
                    Direction = t.Direction.ToString(),
                    Status = t.Status.ToString(),
                    Description = t.Description,
                    ReferenceType = t.ReferenceType,
                    ReferenceId = t.ReferenceId,
                    CreatedAt = t.CreatedAt
                });
            }

            return (items, totalCount);
        }
    }
}
