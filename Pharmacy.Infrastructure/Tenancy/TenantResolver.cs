using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Tenancy.Interfaces;
using Pharmacy.Infrastructure.Persistence;

namespace Pharmacy.Infrastructure.Tenancy;

public sealed class TenantResolver : ITenantResolver
{
    private readonly PharmacyDbContext _context;

    public TenantResolver(PharmacyDbContext context)
    {
        _context = context;
    }

    public async Task<TenantResolution?> ResolveForUserAsync(
        int userId,
        string? tenantSlug,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TenantUsers
            .AsNoTracking()
            .Include(tu => tu.Tenant)
            .Where(tu => tu.UserId == userId && tu.Tenant.IsActive);

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
        return _context.TenantUsers
            .AsNoTracking()
            .AnyAsync(
                tu => tu.UserId == userId && tu.TenantId == tenantId && tu.Tenant.IsActive,
                cancellationToken);
    }
}
