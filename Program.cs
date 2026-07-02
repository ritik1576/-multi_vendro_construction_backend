using InframartAPI_New.Models;
using System.Text;
using InframartAPI_New.Data;
using InframartAPI_New.Repositories;
using InframartAPI_New.Repositories.Interfaces;
using InframartAPI_New.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MultiVendorAPI.Data;
using MultiVendorAPI.Repositories;
using MultiVendorAPI.Repositories.Interfaces;
using MultiVendorAPI.Services;
using MultiVendorAPI.Services.Interfaces;
using InframartAPI_New.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("MySql.EnableLegacyTimestampBehavior", true);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

var serverVersion = ServerVersion.AutoDetect(connectionString);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
// ================= SERVICES =================
builder.Services.AddScoped<IEmailSender, ResendEmailSender>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IVendorKycService, VendorKycService>();

// ================= RAZORPAY CONFIG =================
builder.Services.Configure<RazorpaySettings>(options =>
{
    var keyId = builder.Configuration["RAZORPAY_KEY_ID"] ?? builder.Configuration["Razorpay:KeyId"];
    var keySecret = builder.Configuration["RAZORPAY_KEY_SECRET"] ?? builder.Configuration["Razorpay:KeySecret"];
    options.KeyId = keyId ?? string.Empty;
    options.KeySecret = keySecret ?? string.Empty;
});

// ================= CLOUDFLARE R2 CONFIG =================
builder.Services.Configure<InframartAPI_New.Models.CloudflareR2Settings>(
    builder.Configuration.GetSection("CloudflareR2")
);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();

// Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderServices>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IVendorOrderService, VendorOrderService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.Configure<BrevoSettings>(builder.Configuration.GetSection("BrevoSettings"));
builder.Services.AddHttpClient<IEmailService, BrevoEmailService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["BrevoSettings:ApiKey"];
    if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_BREVO_API_KEY")
    {
        apiKey = config["BREVO_API_KEY"] ?? Environment.GetEnvironmentVariable("BREVO_API_KEY");
    }
    client.BaseAddress = new Uri("https://api.brevo.com/v3/");
    client.DefaultRequestHeaders.Add("api-key", apiKey);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddSwaggerGen(options =>
{
    // Repositories

    // JWT Authentication
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Inframart API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT Token. Example: Bearer {your token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

            ValidAudience =
                    builder.Configuration["Jwt:Audience"],

            IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["Jwt:Key"]!
                        )
                    ),

            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("VendorOnly", policy => policy.RequireRole("vendor"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("customer"));
});

// ================= CORS =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://192.168.10.177:5173",
                "http://192.168.137.1:5173",
                "http://192.168.10.131:5173",
                "https://multi-vendro-construction-frontend.vercel.app"
            )
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();

    });
});

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware
app.UseMiddleware<InframartAPI_New.Middlewares.GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

// CORS must be before Authentication and Authorization
app.UseCors("FrontendPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<InframartAPI_New.Middlewares.ImageProxyMiddleware>();

app.MapControllers();



// Auto-create image_files table if not exists
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<MultiVendorAPI.Data.ApplicationDbContext>();
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS `image_files` (
                `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
                `storage_key` VARCHAR(500) NOT NULL,
                `file_name` VARCHAR(255) NOT NULL,
                `content_type` VARCHAR(100) NOT NULL,
                `vendor_id` BIGINT NOT NULL,
                `created_at` DATETIME NOT NULL
            );
        ");
        Console.WriteLine("Successfully ensured `image_files` table exists.");

        // Ensure email_templates and email_logs tables exist
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS `email_templates` (
                    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
                    `template_key` VARCHAR(100) NOT NULL UNIQUE,
                    `template_name` VARCHAR(255) NOT NULL,
                    `subject` VARCHAR(255) NOT NULL,
                    `html_content` LONGTEXT NOT NULL,
                    `is_active` TINYINT(1) NOT NULL DEFAULT 1,
                    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    `updated_at` DATETIME NULL,
                    `created_by` VARCHAR(255) NULL,
                    `updated_by` VARCHAR(255) NULL
                );
            ");
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS `email_logs` (
                    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
                    `template_id` BIGINT NULL,
                    `recipient_email` VARCHAR(255) NOT NULL,
                    `subject` VARCHAR(255) NOT NULL,
                    `body` LONGTEXT NOT NULL,
                    `status` VARCHAR(50) NOT NULL,
                    `error_message` TEXT NULL,
                    `sent_at` DATETIME NULL,
                    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT `FK_email_logs_email_templates_template_id` FOREIGN KEY (`template_id`) REFERENCES `email_templates` (`id`) ON DELETE SET NULL
                );
            ");
            Console.WriteLine("Successfully ensured `email_templates` and `email_logs` tables exist.");

            // Seed default email templates if empty
            if (!await db.EmailTemplates.AnyAsync())
            {
                db.EmailTemplates.AddRange(new List<EmailTemplate>
                {
                    new EmailTemplate { TemplateKey = "WELCOME_EMAIL", TemplateName = "Welcome Email", Subject = "Welcome to InfraMart", HtmlContent = "Hi {customer_name},<br/><br/>Welcome to InfraMart! Your account is ready.", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new EmailTemplate { TemplateKey = "PASSWORD_RESET", TemplateName = "Password Reset", Subject = "Password Reset Successful", HtmlContent = "Hi {customer_name},<br/><br/>Your password has been reset successfully.", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new EmailTemplate { TemplateKey = "ORDER_CREATED", TemplateName = "Order Created", Subject = "Order #{order_number} Created Successfully", HtmlContent = "Hi {customer_name},<br/><br/>Thank you for your order. Your order number is <strong>#{order_number}</strong> with amount <strong>₹{order_amount}</strong>.", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new EmailTemplate { TemplateKey = "ORDER_CANCELLED", TemplateName = "Order Cancelled", Subject = "Order #{order_number} Cancelled", HtmlContent = "Hi {customer_name},<br/><br/>Your order <strong>#{order_number}</strong> has been cancelled.", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new EmailTemplate { TemplateKey = "ORDER_DELIVERED", TemplateName = "Order Delivered", Subject = "Order #{order_number} Delivered", HtmlContent = "Hi {customer_name},<br/><br/>Good news! Your order <strong>#{order_number}</strong> has been delivered.", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new EmailTemplate { TemplateKey = "VENDOR_APPROVED", TemplateName = "Vendor Approved", Subject = "Vendor Account Approved", HtmlContent = "Hi {vendor_name},<br/><br/>Congratulations! Your vendor profile has been approved.", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new EmailTemplate { TemplateKey = "KYC_APPROVED", TemplateName = "KYC Approved", Subject = "KYC Verification Approved", HtmlContent = "Hi {vendor_name},<br/><br/>Your KYC verification has been approved.", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new EmailTemplate { TemplateKey = "KYC_REJECTED", TemplateName = "KYC Rejected", Subject = "KYC Verification Rejected", HtmlContent = "Hi {vendor_name},<br/><br/>Your KYC verification has been rejected. Reason: {rejection_reason}.", CreatedAt = DateTime.UtcNow, CreatedBy = "System" }
                });
                await db.SaveChangesAsync();
                Console.WriteLine("Seeded default email templates.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking/creating email template tables: {ex.Message}");
        }

        // Ensure Vendors table schema is updated to support integer status and kyc_status
        try
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE `Vendors` ADD COLUMN `kyc_status` INT NOT NULL DEFAULT 1;");
                Console.WriteLine("Added `kyc_status` column to `Vendors`.");
            }
            catch (Exception ex) when (ex.Message.Contains("Duplicate column") || ex.Message.Contains("1060"))
            {
                // Column already exists
            }
            
            // Check if column status needs migrating from string to int
            // Safe conversion
            await db.Database.ExecuteSqlRawAsync("UPDATE `Vendors` SET `status` = '1' WHERE `status` = 'pending' OR `status` IS NULL;");
            await db.Database.ExecuteSqlRawAsync("UPDATE `Vendors` SET `status` = '2' WHERE `status` = 'approved';");
            await db.Database.ExecuteSqlRawAsync("UPDATE `Vendors` SET `status` = '3' WHERE `status` = 'rejected';");
            await db.Database.ExecuteSqlRawAsync("UPDATE `Vendors` SET `status` = '4' WHERE `status` = 'suspended';");
            await db.Database.ExecuteSqlRawAsync("UPDATE `Vendors` SET `status` = '1' WHERE `status` NOT IN ('1', '2', '3', '4');");
            
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE `Vendors` MODIFY COLUMN `status` INT NOT NULL DEFAULT 1;");
            Console.WriteLine("Successfully migrated `Vendors` status and ensured `kyc_status` columns.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking/migrating `Vendors` schema: {ex.Message}");
        }

        // Ensure Orders table schema is updated to support wallet columns
        try
        {
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `orders` ADD COLUMN `subtotal_amount` DECIMAL(18,2) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `orders` ADD COLUMN `commission_amount` DECIMAL(18,2) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `orders` ADD COLUMN `vendor_amount` DECIMAL(18,2) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `orders` ADD COLUMN `final_amount` DECIMAL(18,2) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `orders` ADD COLUMN `razorpay_order_id` VARCHAR(255) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `orders` ADD COLUMN `razorpay_payment_id` VARCHAR(255) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `orders` ADD COLUMN `razorpay_signature` VARCHAR(255) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `orders` ADD COLUMN `payment_gateway` VARCHAR(100) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `orders` ADD COLUMN `payment_method` VARCHAR(100) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `orders` ADD COLUMN `paid_at` DATETIME NULL;"); } catch {}
            Console.WriteLine("Successfully ensured `orders` wallet and Razorpay columns exist.");
            
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `wallet_transactions` ADD COLUMN `title` VARCHAR(255) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `wallet_transactions` ADD COLUMN `gateway_order_id` VARCHAR(255) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `wallet_transactions` ADD COLUMN `gateway_payment_id` VARCHAR(255) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `wallet_transactions` ADD COLUMN `gateway_signature` VARCHAR(255) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `wallet_transactions` ADD COLUMN `payment_gateway` VARCHAR(100) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `wallet_transactions` ADD COLUMN `payment_method` VARCHAR(100) NULL;"); } catch {}
            Console.WriteLine("Successfully ensured `wallet_transactions` title and Razorpay columns exist.");

            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `payments` ADD COLUMN `AddressId` BIGINT NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `payments` ADD COLUMN `CartId` INT NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `payments` ADD COLUMN `CouponCode` VARCHAR(255) NULL;"); } catch {}
            Console.WriteLine("Successfully ensured `payments` state tracking columns exist.");

            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `coupons` ADD COLUMN `per_user_limit` INT NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `notifications` ADD COLUMN `reference_type` VARCHAR(100) NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE `notifications` ADD COLUMN `reference_id` VARCHAR(100) NULL;"); } catch {}
            Console.WriteLine("Successfully ensured `coupons` and `notifications` columns exist.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking/updating `orders` wallet columns: {ex.Message}");
        }

        // Create vendor_kyc table if not exists
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS `vendor_kyc` (
                    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
                    `vendor_id` BIGINT NOT NULL,
                    `business_legal_name` VARCHAR(255) NOT NULL,
                    `bank_account_name` VARCHAR(255) NOT NULL,
                    `aadhaar_document_url` VARCHAR(500) NOT NULL,
                    `gst_number` VARCHAR(50) NOT NULL,
                    `pan_number` VARCHAR(50) NOT NULL,
                    `business_address` TEXT NOT NULL,
                    `bank_account_number` VARCHAR(100) NOT NULL,
                    `ifsc_code` VARCHAR(50) NOT NULL,
                    `gst_certificate_url` VARCHAR(500) NOT NULL,
                    `pan_card_url` VARCHAR(500) NOT NULL,
                    `bank_statement_url` VARCHAR(500) NOT NULL,
                    `status` INT NOT NULL DEFAULT 1,
                    `rejection_reason` VARCHAR(1000) NULL,
                    `submitted_at` DATETIME NULL,
                    `verified_at` DATETIME NULL,
                    `verified_by` BIGINT NULL,
                    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    CONSTRAINT `FK_vendor_kyc_Vendors_vendor_id` FOREIGN KEY (`vendor_id`) REFERENCES `Vendors` (`id`) ON DELETE CASCADE
                );
            ");
            Console.WriteLine("Successfully ensured `vendor_kyc` table exists.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking/creating `vendor_kyc` table: {ex.Message}");
        }
        // Load categories into cache on startup
        try
        {
            var productService = scope.ServiceProvider.GetRequiredService<MultiVendorAPI.Services.Interfaces.IProductService>();
            await productService.GetCategoriesAsync();
            Console.WriteLine("Pre-loaded categories into memory cache.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error pre-loading categories into cache: {ex.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in DB startup script: {ex.Message}");
    }
}

app.Run();



