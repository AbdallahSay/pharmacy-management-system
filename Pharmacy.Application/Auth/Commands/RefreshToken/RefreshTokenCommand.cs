using MediatR;
using Pharmacy.Application.Auth.Contracts;

namespace Pharmacy.Application.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string Token, string? IpAddress, string? UserAgent, string? DeviceInfo) : IRequest<AuthResponseDto>;
