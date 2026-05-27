using InframartAPI_New.Models;

namespace InframartAPI_New.Repositories.Interfaces
{
    public interface IVendorRepository
    {
        Vendor? GetById(int id);
        void AddVendor(Vendor vendor);
        void UpdateVendor(Vendor vendor);
    }
}