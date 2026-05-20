using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Auth.Contracts;
using Pharmacy.Application.Auth.DTOs;
using Pharmacy.Application.Auth.Interfaces;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Application.Common.Interfaces;
using Pharmacy.Application.Common.Validation;
using Pharmacy.Application.Tenancy.Interfaces;
using Pharmacy.Domain.Entities;
using Pharmacy.Infrastructure.Persistence;

namespace Pharmacy.Infrastructure.Auth;

public sealed class IdentityAuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly ITenantResolver _tenantResolver;
    private readonly ITenantContext _tenantContext;
    private readonly PharmacyDbContext _dbContext;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<RegisterDto> _registerValidator;

    public IdentityAuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenGenerator tokenGenerator,
        ITenantResolver tenantResolver,
        ITenantContext tenantContext,
        PharmacyDbContext dbContext,
        IValidator<LoginDto> loginValidator,
        IValidator<RegisterDto> registerValidator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenGenerator = tokenGenerator;
        _tenantResolver = tenantResolver;
        _tenantContext = tenantContext;
        _dbContext = dbContext;
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

        var tenant = await _tenantResolver.ResolveForUserAsync(
            user.Id,
            dto.TenantSlug,
            cancellationToken);

        if (tenant is null)
            throw new UnauthorizedException("Invalid tenant or user membership.");

        return BuildAuthResponse(user, tenant);
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved)
            throw new ForbiddenException("Tenant context is required to register users.");

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

        var tenantUser = new TenantUser
        {
            TenantId = _tenantContext.TenantId,
            UserId = user.Id,
            Role = dto.Role
        };

        _dbContext.TenantUsers.Add(tenantUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var tenant = await _tenantResolver.ResolveForUserAsync(
            user.Id,
            null,
            cancellationToken);

        if (tenant is null)
            throw new ConflictException("User was created but tenant membership is missing.");

        return BuildAuthResponse(user, tenant);
    }

    private AuthResponseDto BuildAuthResponse(ApplicationUser user, TenantResolution tenant)
    {
        var roles = new List<string> { tenant.Role };
        var (accessToken, expiresAt) = _tokenGenerator.Generate(user, roles, tenant.TenantId);

        return new AuthResponseDto(
            accessToken,
            expiresAt,
            user.Email ?? string.Empty,
            user.FullName,
            tenant.TenantId,
            tenant.TenantName,
            roles);
    }

    private static IEnumerable<ValidationFailure> MapIdentityErrors(
        IEnumerable<IdentityError> errors)
    {
        return errors.Select(error => new ValidationFailure(error.Code, error.Description));
    }
}
