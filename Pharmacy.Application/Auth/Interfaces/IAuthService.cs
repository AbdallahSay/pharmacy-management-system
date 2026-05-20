using Pharmacy.Application.Auth.Contracts;
using Pharmacy.Application.Auth.DTOs;

namespace Pharmacy.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);

    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
}
