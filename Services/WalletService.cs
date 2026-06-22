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
                    Title = t.Description ?? string.Empty,
                    ReferenceType = t.ReferenceType,
                    ReferenceId = t.ReferenceId,
                    CreatedAt = t.CreatedAt
                });
            }

            return (items, totalCount);
        }

        public async Task<(bool success, string message, WalletBalanceResponseDto? wallet)> AddMoneyAsync(long userId, AddMoneyRequestDto dto)
        {
            var wallet = await _walletRepository.GetWalletByUserIdAsync(userId);
            if (wallet == null)
            {
                return (false, "Wallet not found.", null);
            }

            var userName = await _walletRepository.GetUserNameAsync(userId) ?? "Customer";
            var balanceBefore = wallet.AvailableBalance;
            var balanceAfter = wallet.AvailableBalance + dto.Amount;

            wallet.AvailableBalance = balanceAfter;
            wallet.TotalCredits += dto.Amount;
            wallet.UpdatedAt = System.DateTime.UtcNow;

            await _walletRepository.UpdateWalletAsync(wallet);

            var txn = new Models.WalletTransaction
            {
                TransactionId = "DEP" + System.Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                WalletId = wallet.Id,
                TransactionType = Models.TransactionType.Deposit,
                Direction = Models.TransactionDirection.Credit,
                Amount = dto.Amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                AvailableBefore = balanceBefore,
                AvailableAfter = balanceAfter,
                LockedBefore = wallet.LockedBalance,
                LockedAfter = wallet.LockedBalance,
                Status = Models.TransactionStatus.Success,
                Description = userName,
                CreatedAt = System.DateTime.UtcNow,
                CreatedBy = "User"
            };

            await _walletRepository.AddTransactionAsync(txn);
            await _walletRepository.SaveChangesAsync();

            var monthlyExpenditure = await _walletRepository.GetMonthlyExpenditureAsync(wallet.Id);

            var response = new WalletBalanceResponseDto
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

            return (true, "Money added successfully.", response);
        }

        public async Task<(bool success, string message, WalletBalanceResponseDto? wallet)> WithdrawMoneyAsync(long userId, WithdrawMoneyRequestDto dto)
        {
            var wallet = await _walletRepository.GetWalletByUserIdAsync(userId);
            if (wallet == null)
            {
                return (false, "Wallet not found.", null);
            }

            if (wallet.AvailableBalance < dto.Amount)
            {
                return (false, "Insufficient wallet balance.", null);
            }

            var balanceBefore = wallet.AvailableBalance;
            var balanceAfter = wallet.AvailableBalance - dto.Amount;

            wallet.AvailableBalance = balanceAfter;
            wallet.TotalDebits += dto.Amount;
            wallet.UpdatedAt = System.DateTime.UtcNow;

            await _walletRepository.UpdateWalletAsync(wallet);

            var txn = new Models.WalletTransaction
            {
                TransactionId = "WDR" + System.Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                WalletId = wallet.Id,
                TransactionType = Models.TransactionType.Withdrawal,
                Direction = Models.TransactionDirection.Debit,
                Amount = dto.Amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                AvailableBefore = balanceBefore,
                AvailableAfter = balanceAfter,
                LockedBefore = wallet.LockedBalance,
                LockedAfter = wallet.LockedBalance,
                Status = Models.TransactionStatus.Success,
                Description = "To Bank Account",
                CreatedAt = System.DateTime.UtcNow,
                CreatedBy = "User"
            };

            await _walletRepository.AddTransactionAsync(txn);
            await _walletRepository.SaveChangesAsync();

            var monthlyExpenditure = await _walletRepository.GetMonthlyExpenditureAsync(wallet.Id);

            var response = new WalletBalanceResponseDto
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

            return (true, "Money withdrawn successfully.", response);
        }
    }
}
