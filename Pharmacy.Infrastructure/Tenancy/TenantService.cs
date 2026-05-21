using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Common.Constants;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Application.Common.Interfaces;
using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Common.Validation;
using Pharmacy.Application.Tenants.Contracts;
using Pharmacy.Application.Tenants.DTOs;
using Pharmacy.Application.Tenants.Interfaces;
using Pharmacy.Domain.Entities;
using Pharmacy.Infrastructure.Persistence;

namespace Pharmacy.Infrastructure.Tenancy;

/// <summary>
/// Creates tenants + their first TenantAdmin user in a single transaction.
/// Uses EF Core's CreateExecutionStrategy to stay compatible with
/// SqlServerRetryingExecutionStrategy (which rejects manual BeginTransaction).
/// </summary>
public sealed class TenantService : ITenantService
{
    private readonly PharmacyDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<CreateTenantDto> _createValidator;
    private readonly ITenantContext _tenantContext;

    public TenantService(
        PharmacyDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IValidator<CreateTenantDto> createValidator,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _createValidator = createValidator;
        _tenantContext = tenantContext;
    }

    public async Task<TenantCreatedResponse> CreateAsync(
        CreateTenantDto dto,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate input — before touching the DB
        await ValidationHelper.ValidateAndThrowAsync(dto, _createValidator, cancellationToken);

        var normalizedSlug = dto.Slug.Trim().ToLowerInvariant();

        // 2. Pre-flight checks (outside transaction — cheap reads)
        var slugExists = await _dbContext.Tenants
            .AnyAsync(t => t.Slug == normalizedSlug, cancellationToken);

        if (slugExists)
            throw new ConflictException($"A pharmacy with slug '{normalizedSlug}' already exists.");

        // 3. Use EF Core's execution strategy so it works with SqlServerRetryingExecutionStrategy.
        //    BeginTransactionAsync() directly is forbidden when retry-on-failure is enabled.
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 4. Create tenant
                var tenant = new Tenant
                {
                    Name = dto.PharmacyName.Trim(),
                    Slug = normalizedSlug,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Tenants.Add(tenant);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // 5. Create admin user via Identity
                var adminUser = new ApplicationUser
                {
                    TenantId = tenant.Id,
                    UserName = BuildTenantUserName(tenant.Id, dto.AdminEmail),
                    Email = dto.AdminEmail.Trim().ToLowerInvariant(),
                    FullName = dto.AdminFullName.Trim(),
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                using (_tenantContext.BeginBypass())
                {
                    var createResult = await _userManager.CreateAsync(adminUser, dto.AdminPassword);

                    if (!createResult.Succeeded)
                    {
                        var errors = createResult.Errors
                            .Select(e => new FluentValidation.Results.ValidationFailure(e.Code, e.Description));
                        throw new ValidationException(errors);
                    }

                    // 6. Link user to the new tenant with a tenant-scoped admin role.
                    _dbContext.TenantUsers.Add(new TenantUser
                    {
                        TenantId = tenant.Id,
                        UserId = adminUser.Id,
                        Role = RoleNames.TenantAdmin
                    });

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                return new TenantCreatedResponse(
                    tenant.Id,
                    tenant.Name,
                    tenant.Slug,
                    adminUser.Id,
                    adminUser.Email!,
                    tenant.CreatedAt);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        });
    }

    public async Task<PagedResponse<TenantListItemDto>> GetPagedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip));

        if (take <= 0 || take > 100)
            throw new ArgumentOutOfRangeException(nameof(take));

        var total = await _dbContext.Tenants
            .CountAsync(cancellationToken);

        var tenants = await _dbContext.Tenants
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(t => new TenantListItemDto(
                t.Id,
                t.Name,
                t.Slug,
                t.IsActive,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<TenantListItemDto>(tenants, skip, take, total);
    }

    private static string BuildTenantUserName(int tenantId, string email)
    {
        return $"{tenantId}:{email.Trim().ToLowerInvariant()}";
    }
}
