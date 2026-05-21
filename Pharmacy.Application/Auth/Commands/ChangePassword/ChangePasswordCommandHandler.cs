using MediatR;
using Microsoft.AspNetCore.Identity;
using Pharmacy.Application.Auth.Interfaces;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Domain.Entities;
using FluentValidation;


namespace Pharmacy.Application.Auth.Commands.ChangePassword;

internal sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthService _authService;

    public ChangePasswordCommandHandler(UserManager<ApplicationUser> userManager, IAuthService authService)
    {
        _userManager = userManager;
        _authService = authService;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        
        if (user == null)
            throw new NotFoundException("User not found." , request.UserId);

        var changeResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        
        if (!changeResult.Succeeded)
        {
            var error = changeResult.Errors.FirstOrDefault()?.Description ?? "Failed to change password.";
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Password", error) });
        }

        
        await _authService.RevokeAllTokensAsync(user.Id, "System", cancellationToken);

        return true;
    }
}
