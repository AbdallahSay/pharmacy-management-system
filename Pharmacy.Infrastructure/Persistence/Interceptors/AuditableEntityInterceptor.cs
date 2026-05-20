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

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (_tenantContext.IsResolved)
                        entry.Entity.TenantId = _tenantContext.TenantId;

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
