namespace Pharmacy.Application.Auth.DTOs;

public sealed class LoginDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    /// <summary>Tenant slug is required so duplicate emails cannot cross tenant boundaries.</summary>
    public string? TenantSlug { get; init; }
}
