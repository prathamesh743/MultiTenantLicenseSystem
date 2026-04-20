using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SharedKernel.Data;
using System.IO;

namespace SharedKernel.Data;

public class LicenseDbContextFactory : IDesignTimeDbContextFactory<LicenseDbContext>
{
    public LicenseDbContext CreateDbContext(string[] args)
    {
        // Load configuration from LicenseService/appsettings.json when running EF tools from LicenseService folder
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json. Check path and file.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<LicenseDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new LicenseDbContext(optionsBuilder.Options);
    }
}