namespace SharedKernel.Data;

public interface ILicenseDbContextTenantFactory
{
    LicenseDbContext CreateForTenant(string tenantId);
    LicenseDbContext CreateSystemContext();
}

