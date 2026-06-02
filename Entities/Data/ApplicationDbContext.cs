using InframartAPI_New.Models;
using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Models;

namespace MultiVendorAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Vendor> Vendors { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().ToTable("products");

            modelBuilder.Entity<Vendor>().ToTable("vendors");

            modelBuilder.Entity<Product>().Property(p => p.Id).HasColumnName("id");
            modelBuilder.Entity<Product>().Property(p => p.VendorId).HasColumnName("vendor_id");
            modelBuilder.Entity<Product>().Property(p => p.CategoryId).HasColumnName("category_id");
            modelBuilder.Entity<Product>().Property(p => p.Name).HasColumnName("name");
            modelBuilder.Entity<Product>().Property(p => p.Slug).HasColumnName("slug");
            modelBuilder.Entity<Product>().Property(p => p.ShortDescription).HasColumnName("short_description");
            modelBuilder.Entity<Product>().Property(p => p.Description).HasColumnName("description");
            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnName("price");
            modelBuilder.Entity<Product>().Property(p => p.DiscountPrice).HasColumnName("discount_price");
            modelBuilder.Entity<Product>().Property(p => p.Sku).HasColumnName("sku");
            modelBuilder.Entity<Product>().Property(p => p.Thumbnail).HasColumnName("thumbnail");
            modelBuilder.Entity<Product>().Property(p => p.Status).HasColumnName("status");
            modelBuilder.Entity<Product>().Property(p => p.InStock).HasColumnName("in_stock");
            modelBuilder.Entity<Product>().Property(p => p.Quantity).HasColumnName("quantity");
            modelBuilder.Entity<Product>().Property(p => p.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<Product>().Property(p => p.UpdatedAt).HasColumnName("updated_at");

            modelBuilder.Entity<Category>().ToTable("categories");
            modelBuilder.Entity<Category>().Property(c => c.Id).HasColumnName("id");
            modelBuilder.Entity<Category>().Property(c => c.ParentId).HasColumnName("parent_id");
            modelBuilder.Entity<Category>().Property(c => c.Name).HasColumnName("name");
            modelBuilder.Entity<Category>().Property(c => c.Slug).HasColumnName("slug");
            modelBuilder.Entity<Category>().Property(c => c.Image).HasColumnName("image");
            modelBuilder.Entity<Category>().Property(c => c.Status).HasColumnName("status");

            modelBuilder.Entity<Cart>(entity =>
            {
                entity.ToTable("carts");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).HasColumnName("id");
                entity.Property(c => c.UserId).HasColumnName("user_id").HasMaxLength(255).IsRequired();
                entity.HasIndex(c => c.UserId).IsUnique();
                entity.HasMany(c => c.CartItems)
                    .WithOne(ci => ci.Cart)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("cart_items");
                entity.HasKey(ci => ci.Id);
                entity.Property(ci => ci.Id).HasColumnName("id");
                entity.Property(ci => ci.CartId).HasColumnName("cart_id");
                entity.Property(ci => ci.ProductId).HasColumnName("product_id");
                entity.Property(ci => ci.Quantity).HasColumnName("quantity");
                entity.HasOne(ci => ci.Product)
                    .WithMany()
                    .HasForeignKey(ci => ci.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(ci => new { ci.CartId, ci.ProductId }).IsUnique();
            });
        }
    }
}
