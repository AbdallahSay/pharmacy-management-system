namespace Pharmacy.Application.Tenants.DTOs;

/// <summary>
/// Input for creating a new pharmacy tenant with its first TenantAdmin user.
/// </summary>
public sealed class CreateTenantDto
{
    /// <summary>Display name of the pharmacy. e.g. "صيدلية النيل"</summary>
    public string PharmacyName { get; init; } = string.Empty;

    /// <summary>
    /// URL-safe unique slug used in login. e.g. "nile-pharmacy"
    /// Lowercase letters, digits, and hyphens only.
    /// </summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>Full name of the first TenantAdmin user for this pharmacy.</summary>
    public string AdminFullName { get; init; } = string.Empty;

    /// <summary>Email of the first TenantAdmin user.</summary>
    public string AdminEmail { get; init; } = string.Empty;

    /// <summary>Password of the first TenantAdmin user.</summary>
    public string AdminPassword { get; init; } = string.Empty;
}
