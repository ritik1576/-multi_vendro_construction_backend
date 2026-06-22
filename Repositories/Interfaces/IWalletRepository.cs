using InframartAPI_New.Models;
using System.Threading.Tasks;

namespace InframartAPI_New.Repositories.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetWalletByUserIdAsync(long userId);
    }
}
