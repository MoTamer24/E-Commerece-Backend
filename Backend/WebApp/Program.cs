using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Infrastructure;
using Application.Interfaces;
using Application.Interfaces.Services;
using Infrastructure.Services;
using Domain.Entities.Identity;
using System.Text;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, MockPaymentService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
// --- Add ASP.NET Core Identity ---
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// --- Add JWT Authentication ---
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Issuer"],
            ValidIssuer = configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
        };
    });


builder.Services.AddControllers();
var app = builder.Build();

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- IMPORTANT: These must be in this order ---
app.UseAuthentication(); // First, who is the user?
app.UseAuthorization();  // Then, what are they allowed to do?



app.MapControllers();

        // 2. Read admin credentials from environment configuration

// Run database migrations and seeding in an isolated scope
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    try 
    {
        // 1. CRITICAL MISSING STEP: Create the database & apply migrations
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Use MigrateAsync() if you are using EF Core Migrations (Add-Migration).
        // If you aren't using migrations yet, use EnsureCreatedAsync() instead.
        await context.Database.MigrateAsync(); 

        // 2. Seed Roles
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var roles = new[] { "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }

        // 3. Seed Admin User
        await DbInitializer.SeedAdminAsync(services, configuration);
    }
    catch (Exception ex)
    {
        // If it still fails, this will actually print the exact error to your console
        Console.WriteLine($"An error occurred during startup: {ex.Message}");
    }
}
app.Run();