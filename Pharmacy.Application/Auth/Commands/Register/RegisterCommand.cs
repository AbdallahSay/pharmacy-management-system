using MediatR;
using Pharmacy.Application.Auth.Contracts;

namespace Pharmacy.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email, 
    string Password,
    string FullName,
    string Role,
    string? IpAddress,
    string? UserAgent,
    string? DeviceInfo) : IRequest<AuthResponseDto>;
