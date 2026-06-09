using InframartAPI_New.Data;
using InframartAPI_New.Models;
using InframartAPI_New.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

public class VendorRepository : IVendorRepository
{
    private readonly AppDbContext _context;

    public VendorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .AnyAsync(x => x.Email == email);
    }

    public async Task AddUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task AddVendorAsync(Vendor vendor)
    {
        await _context.Vendors.AddAsync(vendor);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<(User? user, Vendor? vendor)> GetVendorUserByEmailAsync(string email)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.Role == "vendor");

        if (user == null)
            return (null, null);

        var vendor = await _context.Vendors
            .FirstOrDefaultAsync(v => v.UserId == user.Id);

        return (user, vendor);
    }
}