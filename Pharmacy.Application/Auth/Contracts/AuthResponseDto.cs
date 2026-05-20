namespace Pharmacy.Application.Auth.Contracts;

public sealed record AuthResponseDto(
    string AccessToken,
    DateTime ExpiresAt,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles);
