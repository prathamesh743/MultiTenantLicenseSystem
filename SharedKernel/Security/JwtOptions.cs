namespace SharedKernel.Security;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "LicenseSystem";
    public string Audience { get; init; } = "LicenseSystem";
    public string Key { get; init; } = string.Empty;
}

