using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Models;

public sealed class RenewalJob
{
    private readonly ILicenseDbContextTenantFactory _dbFactory;
    private readonly ILogger<RenewalJob> _logger;

    public RenewalJob(ILicenseDbContextTenantFactory dbFactory, ILogger<RenewalJob> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var systemCtx = _dbFactory.CreateSystemContext();

        var tenants = await systemCtx.Users
            .IgnoreQueryFilters()
            .Select(u => u.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var window = now.AddDays(7);

        foreach (var tenantId in tenants)
        {
            using var ctx = _dbFactory.CreateForTenant(tenantId);

            var expiring = await ctx.Licenses
                .Where(l => l.Status == "Approved" && l.ExpiryDate <= window)
                .ToListAsync(cancellationToken);

            if (expiring.Count == 0) continue;

            foreach (var license in expiring)
            {
                var oldExpiry = license.ExpiryDate;
                var baseDate = oldExpiry > now ? oldExpiry : now;
                license.ExpiryDate = baseDate.AddYears(1);

                ctx.Notifications.Add(new Notification
                {
                    TenantId = tenantId,
                    UserId = license.UserId,
                    CreatedAt = now,
                    IsRead = false,
                    Message = $"License {license.LicenseNumber} auto-renewed. New expiry: {license.ExpiryDate:yyyy-MM-dd}."
                });
            }

            await ctx.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Renewed {Count} licenses for tenant {TenantId}.", expiring.Count, tenantId);
        }
    }
}

