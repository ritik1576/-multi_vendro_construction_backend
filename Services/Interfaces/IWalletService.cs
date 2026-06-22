using InframartAPI_New.DTOs;
using System.Threading.Tasks;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IWalletService
    {
        Task<WalletBalanceResponseDto?> GetWalletBalanceAsync(long userId);
    }
}
