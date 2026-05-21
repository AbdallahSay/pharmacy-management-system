using MediatR;
using Pharmacy.Application.Auth.Interfaces;

namespace Pharmacy.Application.Auth.Commands.RevokeToken;

internal sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
{
    private readonly IAuthService _authService;

    public RevokeTokenCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        return await _authService.RevokeTokenAsync(request.Token, request.IpAddress, cancellationToken);
    }
}
