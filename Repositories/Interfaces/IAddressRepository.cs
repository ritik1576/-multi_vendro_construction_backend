using MultiVendorAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiVendorAPI.Repositories.Interfaces
{
    public interface IAddressRepository
    {
        Task<Address?> GetByIdAsync(long id);
        Task<List<Address>> GetByUserIdAsync(long? userId);
        Task AddAsync(Address address);
        Task UpdateAsync(Address address);
        Task DeleteAsync(Address address);
        Task ResetDefaultAddressesAsync(long? userId);
        Task SaveChangesAsync();
    }
}
