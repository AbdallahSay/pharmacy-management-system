using MediatR;
using Pharmacy.Application.Auth.Contracts;
using Pharmacy.Application.Auth.Interfaces;

namespace Pharmacy.Application.Auth.Commands.RefreshToken;

internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public RefreshTokenCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return await _authService.RefreshTokenAsync(
            request.Token, 
            request.IpAddress, 
            request.UserAgent, 
            request.DeviceInfo, 
            cancellationToken);
    }
}
