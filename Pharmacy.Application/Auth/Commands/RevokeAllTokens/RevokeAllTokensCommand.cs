using MediatR;

namespace Pharmacy.Application.Auth.Commands.RevokeAllTokens;

public sealed record RevokeAllTokensCommand(int UserId, string? IpAddress) : IRequest<bool>;
