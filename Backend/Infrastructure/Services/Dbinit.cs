using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Domain.Entities.Identity;

public static class DbInitializer
{
    public static async Task SeedAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        // 1. Resolve required managers
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();      

        // 2. Read admin credentials from environment configuration
        string? adminEmail = configuration["AdminCredentials__Email"];
        string? adminPassword = configuration["AdminCredentials__Password"];

        // Exit safely if credentials aren't configured
        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            return;
        }

        // 3. Ensure the Admin role exists
        string adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new ApplicationRole(adminRole));
        }
        System.Console.WriteLine($" ###### #### admin email : {adminEmail}");
        System.Console.WriteLine($" ###### #### admin email : {adminPassword}");
        // 4. Create the Admin user if it doesn't already exist
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            // Identity automatically hashes the password securely using PBKDF2
            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, adminRole);
            }
        }
    }
}