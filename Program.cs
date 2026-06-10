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

// Database Contexts
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));
// ================= SERVICES =================
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVendorService, VendorService>();

// ================= RAZORPAY CONFIG =================
builder.Services.Configure<RazorpaySettings>(
    builder.Configuration.GetSection("Razorpay")
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

// Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderServices>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IVendorOrderService, VendorOrderService>();
builder.Services.AddScoped<IAdminService, AdminService>();
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
app.UseHttpsRedirection();

// CORS must be before Authentication and Authorization
app.UseCors("FrontendPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();



app.Run();



