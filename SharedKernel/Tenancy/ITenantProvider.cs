namespace SharedKernel.Tenancy;

public interface ITenantProvider
{
    string? TryGetTenantId();
    string GetTenantIdOrThrow();
}

