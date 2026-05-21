using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Common.Interfaces;
using Pharmacy.Application.Tenancy.Interfaces;
using Pharmacy.Infrastructure.Persistence;

namespace Pharmacy.Infrastructure.Tenancy;

public sealed class TenantResolver : ITenantResolver
{
    private readonly PharmacyDbContext _context;
    private readonly ITenantContext _tenantContext;

    public TenantResolver(
        PharmacyDbContext context,
        ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<TenantResolution?> ResolveForUserAsync(
        int userId,
        string? tenantSlug,
        CancellationToken cancellationToken = default)
    {
        using var bypass = _tenantContext.BeginBypass();

        var query = _context.TenantUsers
            .AsNoTracking()
            .Include(tu => tu.Tenant)
            .Include(tu => tu.User)
            .Where(tu =>
                tu.UserId == userId &&
                tu.User.TenantId == tu.TenantId &&
                tu.Tenant.IsActive);

        if (!string.IsNullOrWhiteSpace(tenantSlug))
        {
            query = query.Where(tu => tu.Tenant.Slug == tenantSlug.Trim().ToLowerInvariant());
        }

        var membership = await query
            .OrderBy(tu => tu.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
            return null;

        return new TenantResolution(
            membership.TenantId,
            membership.Tenant.Name,
            membership.Role);
    }

    public Task<bool> UserBelongsToTenantAsync(
        int userId,
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        using var bypass = _tenantContext.BeginBypass();

        return _context.TenantUsers
            .AsNoTracking()
            .Include(tu => tu.User)
            .AnyAsync(
                tu => tu.UserId == userId &&
                      tu.TenantId == tenantId &&
                      tu.User.TenantId == tenantId &&
                      tu.Tenant.IsActive,
                cancellationToken);
    }

    public async Task<TenantResolution?> ResolveForTenantAsync(
        int userId,
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        using var bypass = _tenantContext.BeginBypass();

        var membership = await _context.TenantUsers
            .AsNoTracking()
            .Include(tu => tu.Tenant)
            .Include(tu => tu.User)
            .Where(tu =>
                tu.UserId == userId &&
                tu.TenantId == tenantId &&
                tu.User.TenantId == tenantId &&
                tu.Tenant.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
            return null;

        return new TenantResolution(
            membership.TenantId,
            membership.Tenant.Name,
            membership.Role);
    }
}
