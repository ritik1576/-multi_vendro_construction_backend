using InframartAPI_New.Models;
using Microsoft.EntityFrameworkCore;

namespace InframartAPI_New.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<OtpVerification> OtpVerifications { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<MultiVendorAPI.Models.ImageFile> ImageFiles { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<VendorKyc> VendorKycs { get; set; }
        public DbSet<Wallet> Wallets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints if needed
            modelBuilder.Entity<Vendor>()
               .HasOne<User>()
               .WithOne()
               .HasForeignKey<Vendor>(v => v.UserId)
               .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
               .HasOne(n => n.User)
               .WithMany()
               .HasForeignKey(n => n.UserId)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorKyc>(entity =>
            {
                entity.ToTable("vendor_kyc");
                entity.HasKey(k => k.Id);
                entity.HasOne<Vendor>()
                      .WithMany()
                      .HasForeignKey(k => k.VendorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}