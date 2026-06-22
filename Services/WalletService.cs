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

            return new WalletBalanceResponseDto
            {
                WalletId = wallet.Id,
                WalletType = wallet.WalletType.ToString(),
                AvailableBalance = wallet.AvailableBalance,
                LockedBalance = wallet.LockedBalance,
                TotalCredits = wallet.TotalCredits,
                TotalDebits = wallet.TotalDebits,
                Status = wallet.Status
            };
        }
    }
}
