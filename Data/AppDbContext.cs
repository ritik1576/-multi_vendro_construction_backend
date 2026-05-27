using Microsoft.EntityFrameworkCore;
using InframartAPI_New.Models;

namespace InframartAPI_New.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<OtpVerification> OtpVerifications { get; set; }
    }
}