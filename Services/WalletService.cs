using InframartAPI_New.Data;
using InframartAPI_New.DTOs;
using InframartAPI_New.Repositories.Interfaces;
using InframartAPI_New.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using InframartAPI_New.Models;
using MultiVendorAPI.Common;

namespace InframartAPI_New.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly AppDbContext _context;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly INotificationService _notificationService;
        private readonly RazorpaySettings _razorpaySettings;

        public WalletService(
            IWalletRepository walletRepository,
            AppDbContext context,
            IEmailNotificationService emailNotificationService,
            INotificationService notificationService,
            IOptions<RazorpaySettings> razorpaySettings)
        {
            _walletRepository = walletRepository;
            _context = context;
            _emailNotificationService = emailNotificationService;
            _notificationService = notificationService;
            _razorpaySettings = razorpaySettings.Value;
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

        public async Task<ServiceResponse<object>> CreateAddMoneyPaymentAsync(long userId, CreateWalletRechargeDto dto)
        {
            await Task.CompletedTask;
            if (string.IsNullOrEmpty(_razorpaySettings.KeyId) || string.IsNullOrEmpty(_razorpaySettings.KeySecret))
            {
                Console.WriteLine("[ERROR] Razorpay configuration is missing or incomplete.");
                return ServiceResponse<object>.FailureResponse("Razorpay Payment Gateway is not configured.", 500);
            }

            if (dto.Amount <= 0)
            {
                return ServiceResponse<object>.FailureResponse("Amount must be greater than zero.", 400);
            }

            long amountInPaise = Convert.ToInt64(dto.Amount * 100);

            try
            {
                var client = new Razorpay.Api.RazorpayClient(_razorpaySettings.KeyId, _razorpaySettings.KeySecret);
                Dictionary<string, object> options = new()
                {
                    { "amount", amountInPaise },
                    { "currency", "INR" },
                    { "receipt", $"WAL-{Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper()}" }
                };

                var razorpayOrder = client.Order.Create(options);
                var razorpayOrderId = razorpayOrder["id"].ToString();

                // Log
                Console.WriteLine($"[INFO] Razorpay Wallet Add Money Order Created: RazorpayOrderId={razorpayOrderId}, UserId={userId}, Amount={dto.Amount}, Timestamp={DateTime.UtcNow}");

                var responseData = new
                {
                    razorpayOrderId = razorpayOrderId,
                    amount = amountInPaise,
                    currency = "INR",
                    key = _razorpaySettings.KeyId
                };

                return ServiceResponse<object>.SuccessResponse(responseData, "Add money payment order created", 201);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Razorpay wallet recharge order creation failed: {ex.Message}");
                return ServiceResponse<object>.FailureResponse($"Razorpay order creation failed: {ex.Message}", 500);
            }
        }

        public async Task<ServiceResponse<object>> VerifyAddMoneyAsync(long userId, VerifyWalletRechargeDto dto)
        {
            if (string.IsNullOrEmpty(_razorpaySettings.KeyId) || string.IsNullOrEmpty(_razorpaySettings.KeySecret))
            {
                Console.WriteLine("[ERROR] Razorpay configuration is missing or incomplete.");
                return ServiceResponse<object>.FailureResponse("Razorpay Payment Gateway is not configured.", 500);
            }

            // Prevent Duplicate Verification: Check if this RazorpayOrderId/PaymentId has already been successfully verified.
            var existingTxn = await _context.WalletTransactions
                .FirstOrDefaultAsync(t => t.GatewayOrderId == dto.RazorpayOrderId || t.GatewayPaymentId == dto.RazorpayPaymentId);
            if (existingTxn != null)
            {
                Console.WriteLine($"[WARNING] Duplicate Verification Attempt: Wallet recharge transaction already exists. RazorpayOrderId={dto.RazorpayOrderId}, UserId={userId}, Timestamp={DateTime.UtcNow}");
                return ServiceResponse<object>.FailureResponse("Payment already verified and wallet credited.", 400);
            }

            // Verify Razorpay Signature
            string secret = _razorpaySettings.KeySecret;
            string payload = dto.RazorpayOrderId + "|" + dto.RazorpayPaymentId;
            string calculatedSignature = "";
            using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret)))
            {
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
                calculatedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            if (calculatedSignature != dto.RazorpaySignature.ToLower())
            {
                Console.WriteLine($"[ERROR] Wallet Recharge Signature Validation Failed: RazorpayOrderId={dto.RazorpayOrderId}, RazorpayPaymentId={dto.RazorpayPaymentId}, UserId={userId}, Timestamp={DateTime.UtcNow}");
                
                try
                {
                    await _notificationService.CreateNotificationAsync(userId, "Wallet Recharge Failed", $"Wallet recharge of {dto.Amount} INR failed.", "wallet");
                }
                catch {}

                return ServiceResponse<object>.FailureResponse("Signature verification failed.", 400);
            }

            // Credit wallet
            var wallet = await _walletRepository.GetWalletByUserIdAsync(userId);
            if (wallet == null)
            {
                return ServiceResponse<object>.FailureResponse("Wallet not found.", 404);
            }

            var balanceBefore = wallet.AvailableBalance;
            var balanceAfter = wallet.AvailableBalance + dto.Amount;

            wallet.AvailableBalance = balanceAfter;
            wallet.TotalCredits += dto.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            await _walletRepository.UpdateWalletAsync(wallet);

            var txn = new Models.WalletTransaction
            {
                TransactionId = "DEP" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
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
                Title = "Wallet Recharge",
                Description = "Money added to wallet via Razorpay",
                GatewayOrderId = dto.RazorpayOrderId,
                GatewayPaymentId = dto.RazorpayPaymentId,
                GatewaySignature = dto.RazorpaySignature,
                PaymentGateway = "Razorpay",
                PaymentMethod = "UPI",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            await _walletRepository.AddTransactionAsync(txn);
            await _walletRepository.SaveChangesAsync();

            // Logs
            Console.WriteLine($"[INFO] Wallet Recharge Payment Verified: RazorpayOrderId={dto.RazorpayOrderId}, RazorpayPaymentId={dto.RazorpayPaymentId}, UserId={userId}, Amount={dto.Amount}, Timestamp={DateTime.UtcNow}");
            Console.WriteLine($"[INFO] Wallet Credited: WalletId={wallet.Id}, UserId={userId}, Amount={dto.Amount}, Timestamp={DateTime.UtcNow}");

            // Send notification & email
            try
            {
                await _notificationService.CreateNotificationAsync(userId, "Wallet Recharge Success", $"Successfully added {dto.Amount} INR to your wallet.", "wallet");
                
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
            catch (Exception exVal)
            {
                Console.WriteLine($"[DEBUG] Notification/Email sending failed for wallet recharge: {exVal.Message}");
            }

            return ServiceResponse<object>.SuccessResponse(new { message = "Wallet recharged successfully.", balance = wallet.AvailableBalance }, "Wallet recharged successfully.");
        }
    }
}
