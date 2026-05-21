using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pharmacy.Application.Common.Interfaces;
using Pharmacy.Domain.Entities;

namespace Pharmacy.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public AuditableEntityInterceptor(
        ITenantContext tenantContext,
        ICurrentUserService currentUserService)
    {
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
            return;

        var utcNow = DateTime.UtcNow;
        int? userId = TryGetUserId();

        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                EnsureTenantContextIsAvailable(entry.Entity);

            if (entry.State == EntityState.Added && !_tenantContext.IsBypassed)
                entry.Entity.TenantId = _tenantContext.TenantId;
        }

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.IsDeleted = false;
                    entry.Entity.DeletedAt = null;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }
    }

    private void EnsureTenantContextIsAvailable(ITenantEntity entity)
    {
        if (_tenantContext.IsBypassed)
        {
            if (entity.TenantId <= 0)
            {
                throw new InvalidOperationException(
                    $"Tenant-owned entity '{entity.GetType().Name}' must have a valid TenantId during tenant bypass operations.");
            }

            return;
        }

        if (!_tenantContext.IsResolved)
        {
            throw new InvalidOperationException(
                $"Tenant context is required to save tenant-owned entity '{entity.GetType().Name}'.");
        }
    }

    private int? TryGetUserId()
    {
        try
        {
            return _currentUserService.GetUserId();
        }
        catch
        {
            return null;
        }
    }
}
