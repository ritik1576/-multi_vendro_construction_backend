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
        public DbSet<Address> Addresses { get; set; }


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

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("order_items");
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Id).HasColumnName("id");
                entity.Property(o => o.OrderId).HasColumnName("order_id");
                entity.Property(o => o.ProductId).HasColumnName("product_id");
                entity.Property(o => o.ProductName).HasColumnName("product_name");
                entity.Property(o => o.Quantity).HasColumnName("quantity");
                entity.Property(o => o.Price).HasColumnName("price");
                entity.Property(o => o.TotalPrice).HasColumnName("total_price");
                entity.Property(o => o.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<Order>().ToTable("orders");
            modelBuilder.Entity<Cart>().ToTable("carts");

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Id).HasColumnName("id");
                entity.Property(o => o.UserId).HasColumnName("user_id");
                entity.Property(o => o.AddressId).HasColumnName("address_id");
                entity.Property(o => o.CouponId).HasColumnName("coupon_id");
                entity.Property(o => o.OrderNumber).HasColumnName("order_number");
                entity.Property(o => o.Subtotal).HasColumnName("subtotal");
                entity.Property(o => o.DiscountAmount).HasColumnName("discount_amount");
                entity.Property(o => o.ShippingCharge).HasColumnName("shipping_charge");
                entity.Property(o => o.TotalAmount).HasColumnName("total_amount");
                entity.Property(o => o.PaymentStatus).HasColumnName("payment_status");
                entity.Property(o => o.OrderStatus).HasColumnName("order_status");
                entity.Property(o => o.PlacedAt).HasColumnName("placed_at");
                entity.Property(o => o.CreatedAt).HasColumnName("created_at");
                entity.Ignore(o => o.OrderDate);
                entity.HasMany(o => o.OrderItems)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId);
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

            modelBuilder.Entity<Address>(entity =>
            {
                entity.ToTable("addresses");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Id).HasColumnName("id");
                entity.Property(a => a.UserId).HasColumnName("user_id");
                entity.Property(a => a.FullName).HasColumnName("full_name");
                entity.Property(a => a.Phone).HasColumnName("phone");
                entity.Property(a => a.AddressLine1).HasColumnName("address_line1");
                entity.Property(a => a.AddressLine2).HasColumnName("address_line2");
                entity.Property(a => a.City).HasColumnName("city");
                entity.Property(a => a.State).HasColumnName("state");
                entity.Property(a => a.Country).HasColumnName("country");
                entity.Property(a => a.PostalCode).HasColumnName("postal_code");
                entity.Property(a => a.AddressType).HasColumnName("address_type");
                entity.Property(a => a.IsDefault).HasColumnName("is_default");
                entity.Property(a => a.CreatedAt).HasColumnName("created_at");
            });
        }
    }
}
