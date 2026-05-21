using MediatR;
using Pharmacy.Application.Auth.Contracts;

namespace Pharmacy.Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email, 
    string Password,
    string? IpAddress,
    string? UserAgent,
    string? DeviceInfo) : IRequest<AuthResponseDto>;
