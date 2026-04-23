using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SharedKernel.Tenancy;

public sealed class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? TryGetTenantId()
        => _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;

    public string GetTenantIdOrThrow()
    {
        var tenantId = TryGetTenantId();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new UnauthorizedAccessException("TenantId claim is missing.");
        }

        return tenantId;
    }
}

