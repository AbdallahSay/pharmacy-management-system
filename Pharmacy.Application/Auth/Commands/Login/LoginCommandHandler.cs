using MediatR;
using Microsoft.AspNetCore.Identity;
using Pharmacy.Application.Auth.Contracts;
using Pharmacy.Application.Auth.Interfaces;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Domain.Entities;

namespace Pharmacy.Application.Auth.Commands.Login;

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IAuthService _authService; 

    // We use IAuthService here to reuse BuildAuthResponseAsync which handles token generation
    // Alternatively, we refactor IAuthService out. For safety, we'll keep using IAuthService 
    // for the complex response generation if we don't want to duplicate Jwt logic here.
    // Actually, the plan says: "Refactor Auth flow so ALL operations use MediatR: LoginCommand...".
    // I will call IAuthService.LoginAsync from here, but update it to accept UserAgent/DeviceInfo.

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // To preserve existing IdentityAuthService logic and avoid breaking changes immediately,
        // we delegate to it. We will update IAuthService to accept these new fields.
        return await _authService.LoginAsync(
            new DTOs.LoginDto
            {
                Email = request.Email,
                Password = request.Password,
            },
            request.IpAddress, 
            request.UserAgent,
            request.DeviceInfo,
            cancellationToken);
    }
}
