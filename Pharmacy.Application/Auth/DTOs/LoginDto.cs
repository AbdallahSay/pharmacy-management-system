namespace Pharmacy.Application.Auth.DTOs;

public sealed class LoginDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    /// <summary>Optional. Uses default tenant membership when omitted.</summary>
    public string? TenantSlug { get; init; }
}
