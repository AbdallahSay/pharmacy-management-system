namespace Pharmacy.Application.Auth.Contracts;

public sealed record AuthResponseDto(
    string AccessToken,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles);
