using MediatR;

namespace Pharmacy.Application.Auth.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string Token, string? IpAddress) : IRequest<bool>;
