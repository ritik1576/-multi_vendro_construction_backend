using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Data;
using MultiVendorAPI.Models;
using MultiVendorAPI.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiVendorAPI.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly ApplicationDbContext _context;

        public AddressRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Address?> GetByIdAsync(long id)
        {
            return await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<Address>> GetByUserIdAsync(long? userId)
        {
            return await _context.Addresses
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }

        public async Task AddAsync(Address address)
        {
            await _context.Addresses.AddAsync(address);
        }

        public Task UpdateAsync(Address address)
        {
            _context.Addresses.Update(address);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Address address)
        {
            address.UserId = null;
            return Task.CompletedTask;
        }

        public async Task ResetDefaultAddressesAsync(long? userId)
        {
            var defaultAddresses = await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync();

            foreach (var addr in defaultAddresses)
            {
                addr.IsDefault = false;
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
