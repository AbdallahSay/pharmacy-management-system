namespace Pharmacy.Application.Tenancy.Interfaces;

public interface ITenantResolver
{
    Task<TenantResolution?> ResolveForUserAsync(
        int userId,
        string? tenantSlug,
        CancellationToken cancellationToken = default);

    Task<bool> UserBelongsToTenantAsync(
        int userId,
        int tenantId,
        CancellationToken cancellationToken = default);

    Task<TenantResolution?> ResolveForTenantAsync(
        int userId,
        int tenantId,
        CancellationToken cancellationToken = default);
}

public sealed record TenantResolution(int TenantId, string TenantName, string Role);
