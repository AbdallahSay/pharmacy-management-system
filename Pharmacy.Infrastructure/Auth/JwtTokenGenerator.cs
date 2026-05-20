using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pharmacy.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Pharmacy.Infrastructure.Auth;

public sealed class JwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public (string AccessToken, DateTime ExpiresAt) Generate(ApplicationUser user, IList<string> roles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_settings.Key);
        ArgumentException.ThrowIfNullOrWhiteSpace(_settings.Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(_settings.Audience);

        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiresInMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
