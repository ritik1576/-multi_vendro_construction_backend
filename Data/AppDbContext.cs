using InframartAPI_New.Models;
using Microsoft.EntityFrameworkCore;

namespace InframartAPI_New.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Vendor> Vendors => Set<Vendor>();
        
        public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();

        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    }
}