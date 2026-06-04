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
        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().ToTable("products");

            modelBuilder.Entity<Vendor>(entity =>
            {
                entity.ToTable("vendors");
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Id).HasColumnName("id");
                entity.Property(v => v.UserId).HasColumnName("user_id");
                entity.Property(v => v.shop_name).HasColumnName("shop_name");
                entity.Property(v => v.ShopSlug).HasColumnName("shop_slug");
                entity.Property(v => v.Description).HasColumnName("description");
                entity.Property(v => v.Logo).HasColumnName("logo");
                entity.Property(v => v.Banner).HasColumnName("banner");
                entity.Property(v => v.GstNumber).HasColumnName("gst_number");
                entity.Property(v => v.CommissionRate).HasColumnName("commission_rate");
                entity.Property(v => v.Status).HasColumnName("status");
                entity.Property(v => v.CreatedAt).HasColumnName("created_at");
                entity.Property(v => v.UpdatedAt).HasColumnName("updated_at");
            });

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
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Vendor)
                .WithMany()
                .HasForeignKey(p => p.VendorId);

            modelBuilder.Entity<Category>().ToTable("categories");
            modelBuilder.Entity<Category>().Property(c => c.Id).HasColumnName("id");
            modelBuilder.Entity<Category>().Property(c => c.ParentId).HasColumnName("parent_id");
            modelBuilder.Entity<Category>().Property(c => c.Name).HasColumnName("name");
            modelBuilder.Entity<Category>().Property(c => c.Slug).HasColumnName("slug");
            modelBuilder.Entity<Category>().Property(c => c.Image).HasColumnName("image");
            modelBuilder.Entity<Category>().Property(c => c.Status).HasColumnName("status");
            modelBuilder.Entity<Cart>().Property(c => c.UserId).HasColumnName("user_id");

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

            modelBuilder.Entity<OrderItem>().ToTable("order_items");

            modelBuilder.Entity<OrderItem>()
                .HasKey(o => o.Id);

            modelBuilder.Entity<Order>().ToTable("orders");
            modelBuilder.Entity<Cart>().ToTable("carts");

            modelBuilder.Entity<Order>()
                .HasKey(o => o.Id);

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
