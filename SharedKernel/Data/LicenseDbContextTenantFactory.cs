using Microsoft.EntityFrameworkCore;

namespace SharedKernel.Data;

public sealed class LicenseDbContextTenantFactory : ILicenseDbContextTenantFactory
{
    private readonly DbContextOptions<LicenseDbContext> _options;

    public LicenseDbContextTenantFactory(DbContextOptions<LicenseDbContext> options)
    {
        _options = options;
    }

    public LicenseDbContext CreateForTenant(string tenantId)
    {
        var ctx = new LicenseDbContext(_options) { TenantId = tenantId };
        return ctx;
    }

    public LicenseDbContext CreateSystemContext()
    {
        // Use a sentinel tenant id; queries that need cross-tenant data must use IgnoreQueryFilters().
        var ctx = new LicenseDbContext(_options) { TenantId = "__system__" };
        return ctx;
    }
}

