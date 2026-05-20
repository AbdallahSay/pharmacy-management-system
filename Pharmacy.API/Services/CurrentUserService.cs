using System.Security.Claims;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Application.Common.Interfaces;

namespace Pharmacy.API.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
            ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("User is not authenticated.");

        return userId;
    }
}
