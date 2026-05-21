using MediatR;
using Pharmacy.Application.Auth.Interfaces;

namespace Pharmacy.Application.Auth.Commands.RevokeAllTokens;

internal sealed class RevokeAllTokensCommandHandler : IRequestHandler<RevokeAllTokensCommand, bool>
{
    private readonly IAuthService _authService;

    public RevokeAllTokensCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<bool> Handle(RevokeAllTokensCommand request, CancellationToken cancellationToken)
    {
        return await _authService.RevokeAllTokensAsync(request.UserId, request.IpAddress, cancellationToken);
    }
}
