using InframartAPI_New.Models;

namespace InframartAPI_New.Repositories.Interfaces
{
    public interface IVendorRepository
    {
        Task<bool> EmailExistsAsync(string email);

        Task AddUserAsync(User user);

        Task AddVendorAsync(Vendor vendor);

        Task SaveChangesAsync();

        /// <summary>
        /// Returns the User whose email matches AND whose role is "vendor",
        /// along with the associated Vendor profile (or null if not found).
        /// </summary>
        Task<(User? user, Vendor? vendor)> GetVendorUserByEmailAsync(string email);
    }
}