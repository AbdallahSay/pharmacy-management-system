using Pharmacy.Application.Auth.Contracts;
using Pharmacy.Application.Auth.DTOs;

namespace Pharmacy.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ipAddress, string? userAgent, string? deviceInfo, CancellationToken cancellationToken = default);

    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string? ipAddress, string? userAgent, string? deviceInfo, CancellationToken cancellationToken = default);

    Task<AuthResponseDto> RefreshTokenAsync(string token, string? ipAddress, string? userAgent, string? deviceInfo, CancellationToken cancellationToken = default);

    Task<bool> RevokeTokenAsync(string token, string? ipAddress, CancellationToken cancellationToken = default);

    Task<bool> RevokeAllTokensAsync(int userId, string? ipAddress, CancellationToken cancellationToken = default);
}
