using Microsoft.EntityFrameworkCore;
using InframartAPI.Models;

namespace InframartAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vendor> Vendors { get; set; }
    }
}