namespace Pharmacy.Application.Auth.Contracts;

public sealed record AuthResponseDto(
    string AccessToken,
    DateTime ExpiresAt,
    string Email,
    string FullName,
    int TenantId,
    string TenantName,
    IReadOnlyList<string> Roles);
