using MediatR;
using Pharmacy.Application.Auth.Contracts;
using Pharmacy.Application.Auth.DTOs;
using Pharmacy.Application.Auth.Interfaces;

namespace Pharmacy.Application.Auth.Commands.Register;

internal sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return await _authService.RegisterAsync(
            new RegisterDto
            {
                Email = request.Email,
                Password = request.Password,
                FullName = request.FullName,
                Role = request.Role
            },
            request.IpAddress,
            request.UserAgent,
            request.DeviceInfo,
            cancellationToken);
    }
}
