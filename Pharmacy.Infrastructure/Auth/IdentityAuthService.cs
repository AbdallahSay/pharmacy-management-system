using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
    private readonly PharmacyDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<RegisterDto> _registerValidator;

    public IdentityAuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenGenerator tokenGenerator,
        PharmacyDbContext dbContext,
        IOptions<JwtSettings> jwtSettings,
        IValidator<LoginDto> loginValidator,
        IValidator<RegisterDto> registerValidator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenGenerator = tokenGenerator;
        _dbContext = dbContext;
        _jwtSettings = jwtSettings.Value;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginDto dto,
        string? ipAddress,
        string? userAgent,
        string? deviceInfo,
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

        return await BuildAuthResponseAsync(user, ipAddress, userAgent, deviceInfo, cancellationToken);
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto dto,
        string? ipAddress,
        string? userAgent,
        string? deviceInfo,
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

        return await BuildAuthResponseAsync(user, ipAddress, userAgent, deviceInfo, cancellationToken);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(
        string token,
        string? ipAddress,
        string? userAgent,
        string? deviceInfo,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = TokenGeneratorService.HashToken(token);

        var refreshToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken == null)
            throw new UnauthorizedException("Invalid refresh token.");

        // Security: Token Family Reuse Detection
        if (refreshToken.IsRevoked)
        {
            // Revoke entire token family (all descendants and active tokens in this family)
            await RevokeFamilyTokens(refreshToken.FamilyId, ipAddress, "Attempted reuse of revoked token", cancellationToken);
            throw new UnauthorizedException("Token has been revoked.");
        }

        if (refreshToken.IsExpired)
        {
            throw new UnauthorizedException("Refresh token has expired.");
        }

        // Update LastUsedAt for the current token being exchanged
        refreshToken.LastUsedAt = DateTime.UtcNow;

        // Rotate the token
        var newRefreshTokenStr = TokenGeneratorService.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            TokenHash = TokenGeneratorService.HashToken(newRefreshTokenStr),
            UserId = refreshToken.UserId,
            TenantId = refreshToken.TenantId,
            FamilyId = refreshToken.FamilyId, // Maintain family ID
            ParentTokenId = refreshToken.Id,  // Link to parent
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiresInDays),
            CreatedByIp = ipAddress,
            UserAgent = userAgent,
            DeviceInfo = deviceInfo
        };

        // Revoke the old token
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReasonRevoked = "Replaced by new token";

        _dbContext.RefreshTokens.Add(newRefreshToken);
        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = await _userManager.GetRolesAsync(refreshToken.User);
        var (accessToken, expiresAt) = _tokenGenerator.Generate(refreshToken.User, roles);

        return new AuthResponseDto(
            accessToken,
            expiresAt,
            newRefreshTokenStr,
            newRefreshToken.ExpiresAt,
            refreshToken.User.Email ?? string.Empty,
            refreshToken.User.FullName,
            roles.ToList());
    }

    public async Task<bool> RevokeTokenAsync(string token, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = TokenGeneratorService.HashToken(token);

        var refreshToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken == null || !refreshToken.IsActive)
            return false;

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReasonRevoked = "Revoked by user";

        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RevokeAllTokensAsync(int userId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = "Revoked all sessions";
            _dbContext.RefreshTokens.Update(token);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(
        ApplicationUser user, 
        string? ipAddress, 
        string? userAgent, 
        string? deviceInfo, 
        CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = _tokenGenerator.Generate(user, roles);

        var refreshTokenStr = TokenGeneratorService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            TokenHash = TokenGeneratorService.HashToken(refreshTokenStr),
            UserId = user.Id,
            TenantId = user.TenantId,
            FamilyId = Guid.NewGuid(), // Start a new token family
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiresInDays),
            CreatedByIp = ipAddress,
            UserAgent = userAgent,
            DeviceInfo = deviceInfo
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            accessToken,
            expiresAt,
            refreshTokenStr,
            refreshToken.ExpiresAt,
            user.Email ?? string.Empty,
            user.FullName,
            roles.ToList());
    }

    private async Task RevokeFamilyTokens(Guid familyId, string? ipAddress, string reason, CancellationToken cancellationToken)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(x => x.FamilyId == familyId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = reason;
            _dbContext.RefreshTokens.Update(token);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<ValidationFailure> MapIdentityErrors(
        IEnumerable<IdentityError> errors)
    {
        return errors.Select(error => new ValidationFailure(error.Code, error.Description));
    }
}
