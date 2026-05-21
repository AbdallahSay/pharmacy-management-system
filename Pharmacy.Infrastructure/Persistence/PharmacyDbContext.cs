using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Common.Interfaces;
using Pharmacy.Domain.Entities;
using System.Linq.Expressions;
using System.Reflection;

namespace Pharmacy.Infrastructure.Persistence;

public class PharmacyDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    private readonly ITenantContext _tenantContext;

    public PharmacyDbContext(
        DbContextOptions<PharmacyDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        ApplyTenantAndSoftDeleteFilters(modelBuilder);
    }

    private void ApplyTenantAndSoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var methodName = typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)
                ? nameof(SetTenantAndSoftDeleteFilter)
                : typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType)
                    ? nameof(SetTenantFilter)
                    : null;

            if (methodName is null)
                continue;

            var method = typeof(PharmacyDbContext)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);
            method.Invoke(this, [modelBuilder]);
        }
    }

    private void SetTenantAndSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : BaseEntity
    {
        Expression<Func<TEntity, bool>> filter = entity =>
            !entity.IsDeleted &&
            (_tenantContext.IsBypassed ||
             (_tenantContext.IsResolved && entity.TenantId == _tenantContext.TenantId));

        modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity
    {
        Expression<Func<TEntity, bool>> filter = entity =>
            _tenantContext.IsBypassed ||
            (_tenantContext.IsResolved && entity.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
    }
}
