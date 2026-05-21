using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Application.Common.Interfaces;
using Pharmacy.Application.Tenancy.Interfaces;
using Pharmacy.Domain.Common;
using System.Security.Claims;

namespace Pharmacy.API.Middleware;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ITenantResolver tenantResolver)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirstValue(TenantClaimTypes.TenantId);

            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out var tenantId))
                throw new UnauthorizedException("Tenant context is missing from the token.");

            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("User context is missing from the token.");

            var tenant = await tenantResolver.ResolveForTenantAsync(userId, tenantId, context.RequestAborted);

            if (tenant is null)
                throw new ForbiddenException("You do not have access to this tenant.");

            var roles = context.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value);

            if (!roles.Contains(tenant.Role, StringComparer.Ordinal))
                throw new ForbiddenException("Token role is not valid for this tenant.");

            tenantContext.SetTenant(tenantId);
        }

        await _next(context);
    }
}
