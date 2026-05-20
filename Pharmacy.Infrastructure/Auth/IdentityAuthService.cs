using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Pharmacy.Application.Auth.Contracts;
using Pharmacy.Application.Auth.DTOs;
using Pharmacy.Application.Auth.Interfaces;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Application.Common.Validation;
using Pharmacy.Domain.Entities;

namespace Pharmacy.Infrastructure.Auth;

public sealed class IdentityAuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<RegisterDto> _registerValidator;

    public IdentityAuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenGenerator tokenGenerator,
        IValidator<LoginDto> loginValidator,
        IValidator<RegisterDto> registerValidator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenGenerator = tokenGenerator;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(dto, _loginValidator, cancellationToken);

        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is null)
            throw new UnauthorizedException();

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            dto.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
            throw new UnauthorizedException();

        return await BuildAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(dto, _registerValidator, cancellationToken);

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);

        if (existingUser is not null)
            throw new ConflictException($"A user with email '{dto.Email}' already exists.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName.Trim(),
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);

        if (!createResult.Succeeded)
            throw new ValidationException(MapIdentityErrors(createResult.Errors));

        var roleResult = await _userManager.AddToRoleAsync(user, dto.Role);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw new ValidationException(MapIdentityErrors(roleResult.Errors));
        }

        return await BuildAuthResponseAsync(user);
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = _tokenGenerator.Generate(user, roles);

        return new AuthResponseDto(
            accessToken,
            expiresAt,
            user.Email ?? string.Empty,
            user.FullName,
            roles.ToList());
    }

    private static IEnumerable<ValidationFailure> MapIdentityErrors(
        IEnumerable<IdentityError> errors)
    {
        return errors.Select(error => new ValidationFailure(error.Code, error.Description));
    }
}
