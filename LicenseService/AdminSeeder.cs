using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SharedKernel.Data;
using SharedKernel.Models;

public static class AdminSeeder
{
    public static void Seed(IServiceProvider services, IHostEnvironment env)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");

        var username = config["Seed:AdminUsername"];
        var password = config["Seed:AdminPassword"];
        var tenantId = config["Seed:AdminTenantId"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(tenantId))
        {
            if (env.IsDevelopment() || string.Equals(env.EnvironmentName, "Docker", StringComparison.OrdinalIgnoreCase))
            {
                username ??= "admin";
                password ??= "Admin123!";
                tenantId ??= "tenant1";
            }
            else
            {
                logger.LogWarning("Admin seed skipped. Configure Seed:AdminUsername/AdminPassword/AdminTenantId to enable seeding.");
                return;
            }
        }

        var ctx = services.GetRequiredService<LicenseDbContext>();

        var exists = ctx.Users.IgnoreQueryFilters().Any(u => u.Username == username && u.TenantId == tenantId);
        if (exists) return;

        ctx.Users.Add(new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Admin",
            TenantId = tenantId,
            Agency = null
        });

        ctx.SaveChanges();
        logger.LogInformation("Seeded admin user '{Username}' for tenant '{TenantId}'.", username, tenantId);
    }
}

