using InframartAPI_New.Data;
using InframartAPI_New.DTOs;
using InframartAPI_New.Repositories.Interfaces;
using InframartAPI_New.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace InframartAPI_New.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly AppDbContext _context;
        private readonly IEmailNotificationService _emailNotificationService;

        public WalletService(
            IWalletRepository walletRepository,
            AppDbContext context,
            IEmailNotificationService emailNotificationService)
        {
            _walletRepository = walletRepository;
            _context = context;
            _emailNotificationService = emailNotificationService;
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
                    Title = t.Title ?? t.Description ?? string.Empty,
                    Description = t.Description,
                    ReferenceType = t.ReferenceType,
                    ReferenceId = t.ReferenceId,
                    CreatedAt = Helpers.TimezoneHelper.ConvertToIst(t.CreatedAt)
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

            if (dto.Amount > 100000m)
            {
                return (false, "Deposit amount cannot exceed 100,000.", null);
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
                Title = userName,
                Description = "Money added to wallet",
                CreatedAt = System.DateTime.UtcNow,
                CreatedBy = "User"
            };

            await _walletRepository.AddTransactionAsync(txn);
            await _walletRepository.SaveChangesAsync();

            // Trigger ADD_MONEY email notification
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailNotificationService.SendTemplateEmailAsync(
                        "ADD_MONEY",
                        user.Email,
                        new Dictionary<string, string>
                        {
                            { "CustomerName", user.FullName ?? "Customer" },
                            { "Amount", dto.Amount.ToString("F2") },
                            { "WalletBalance", wallet.AvailableBalance.ToString("F2") },
                            { "TransactionId", txn.TransactionId },
                            { "WalletUrl", "https://inframart.com/wallet" }
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send add money email: {ex.Message}");
            }

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
                Title = "To Bank Account",
                Description = "Withdrawal from wallet",
                CreatedAt = System.DateTime.UtcNow,
                CreatedBy = "User"
            };

            await _walletRepository.AddTransactionAsync(txn);
            await _walletRepository.SaveChangesAsync();

            // Trigger WITHDRAW email notification
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailNotificationService.SendTemplateEmailAsync(
                        "WITHDRAW",
                        user.Email,
                        new Dictionary<string, string>
                        {
                            { "CustomerName", user.FullName ?? "Customer" },
                            { "Amount", dto.Amount.ToString("F2") },
                            { "WalletBalance", wallet.AvailableBalance.ToString("F2") },
                            { "TransactionId", txn.TransactionId },
                            { "WalletUrl", "https://inframart.com/wallet" }
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send withdraw email: {ex.Message}");
            }

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

        public async Task<(bool success, string message, WalletBalanceResponseDto? wallet)> TransferMoneyAsync(long userId, TransferMoneyRequestDto dto)
        {
            var customerWallet = await _walletRepository.GetWalletByUserIdAsync(userId);
            if (customerWallet == null)
            {
                return (false, "Sender wallet not found.", null);
            }

            var vendorWallet = await _walletRepository.GetWalletByUserIdAsync(dto.TargetUserId);
            if (vendorWallet == null)
            {
                return (false, "Recipient wallet not found.", null);
            }

            if (customerWallet.AvailableBalance < dto.Amount)
            {
                return (false, "Insufficient wallet balance.", null);
            }

            var customerName = await _walletRepository.GetUserNameAsync(userId) ?? "Customer";
            var vendorName = await _walletRepository.GetVendorNameByUserIdAsync(dto.TargetUserId) ?? "Vendor";

            var custBefore = customerWallet.AvailableBalance;
            var custAfter = customerWallet.AvailableBalance - dto.Amount;

            var vendBefore = vendorWallet.AvailableBalance;
            var vendAfter = vendorWallet.AvailableBalance + dto.Amount;

            // Update sender (customer) wallet
            customerWallet.AvailableBalance = custAfter;
            customerWallet.TotalDebits += dto.Amount;
            customerWallet.UpdatedAt = System.DateTime.UtcNow;
            await _walletRepository.UpdateWalletAsync(customerWallet);

            // Update recipient (vendor) wallet
            vendorWallet.AvailableBalance = vendAfter;
            vendorWallet.TotalCredits += dto.Amount;
            vendorWallet.UpdatedAt = System.DateTime.UtcNow;
            await _walletRepository.UpdateWalletAsync(vendorWallet);

            // Generate transaction id
            var transactionId = "TRF" + System.Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

            // Sender debit transaction (Title is Recipient Vendor Name)
            var senderTxn = new Models.WalletTransaction
            {
                TransactionId = transactionId,
                WalletId = customerWallet.Id,
                TransactionType = Models.TransactionType.Transfer,
                Direction = Models.TransactionDirection.Debit,
                Amount = dto.Amount,
                BalanceBefore = custBefore,
                BalanceAfter = custAfter,
                AvailableBefore = custBefore,
                AvailableAfter = custAfter,
                LockedBefore = customerWallet.LockedBalance,
                LockedAfter = customerWallet.LockedBalance,
                Status = Models.TransactionStatus.Success,
                Title = vendorName,
                Description = $"Transfer to vendor {vendorName}",
                CreatedAt = System.DateTime.UtcNow,
                CreatedBy = "User"
            };

            // Recipient credit transaction (Title is Sender Customer Name)
            var recipientTxn = new Models.WalletTransaction
            {
                TransactionId = transactionId,
                WalletId = vendorWallet.Id,
                TransactionType = Models.TransactionType.Transfer,
                Direction = Models.TransactionDirection.Credit,
                Amount = dto.Amount,
                BalanceBefore = vendBefore,
                BalanceAfter = vendAfter,
                AvailableBefore = vendBefore,
                AvailableAfter = vendAfter,
                LockedBefore = vendorWallet.LockedBalance,
                LockedAfter = vendorWallet.LockedBalance,
                Status = Models.TransactionStatus.Success,
                Title = customerName,
                Description = $"Transfer from customer {customerName}",
                CreatedAt = System.DateTime.UtcNow,
                CreatedBy = "User"
            };

            await _walletRepository.AddTransactionAsync(senderTxn);
            await _walletRepository.AddTransactionAsync(recipientTxn);
            await _walletRepository.SaveChangesAsync();

            // Trigger TRANSFER email notification to customer (sender)
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailNotificationService.SendTemplateEmailAsync(
                        "TRANSFER",
                        user.Email,
                        new Dictionary<string, string>
                        {
                            { "CustomerName", user.FullName ?? "Customer" },
                            { "Amount", dto.Amount.ToString("F2") },
                            { "WalletBalance", customerWallet.AvailableBalance.ToString("F2") },
                            { "TransactionId", transactionId },
                            { "WalletUrl", "https://inframart.com/wallet" }
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send wallet transfer email: {ex.Message}");
            }

            var monthlyExpenditure = await _walletRepository.GetMonthlyExpenditureAsync(customerWallet.Id);

            var response = new WalletBalanceResponseDto
            {
                WalletId = customerWallet.Id,
                WalletType = customerWallet.WalletType.ToString(),
                AvailableBalance = customerWallet.AvailableBalance,
                LockedBalance = customerWallet.LockedBalance,
                TotalCredits = customerWallet.TotalCredits,
                TotalDebits = customerWallet.TotalDebits,
                MonthlyExpenditure = monthlyExpenditure,
                Status = customerWallet.Status
            };

            return (true, "Money transferred successfully.", response);
        }
    }
}
