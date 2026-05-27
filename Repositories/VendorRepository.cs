using InframartAPI_New.Data;
using InframartAPI_New.Models;
using InframartAPI_New.Repositories.Interfaces;

namespace InframartAPI_New.Repositories
{
    public class VendorRepository : IVendorRepository
    {
        private readonly AppDbContext _context;

        public VendorRepository(AppDbContext context)
        {
            _context = context;
        }

        public Vendor? GetById(int id)
        {
            return _context.Vendors.FirstOrDefault(x => x.Id == id);
        }

        public void AddVendor(Vendor vendor)
        {
            _context.Vendors.Add(vendor);
            _context.SaveChanges();
        }

        public void UpdateVendor(Vendor vendor)
        {
            _context.Vendors.Update(vendor);
            _context.SaveChanges();
        }
    }
}