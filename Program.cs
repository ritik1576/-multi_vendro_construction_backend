using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

using InframartAPI_New.Data;
using InframartAPI_New.Middlewares;
using InframartAPI_New.Repositories;
using InframartAPI_New.Repositories.Interfaces;
using InframartAPI_New.Services;
using InframartAPI_New.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

#region SERVICES

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Inframart API",
        Version = "v1"
    });
});

#endregion

#region DATABASE (MYSQL)

// MySQL DB Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

#endregion

#region DEPENDENCY INJECTION

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVendorService, VendorService>();

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();

#endregion

var app = builder.Build();

#region MIDDLEWARE PIPELINE

// Swagger (only in dev)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inframart API v1");
    });
}

// Custom JWT Middleware
app.UseMiddleware<JwtMiddleware>();

app.UseAuthorization();

app.MapControllers();

#endregion

app.Run();