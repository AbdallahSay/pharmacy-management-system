using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pharmacy.Application.Common.Constants;
using Pharmacy.Domain.Entities;
using Pharmacy.Infrastructure.Persistence;

namespace Pharmacy.Infrastructure.Identity;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<PharmacyDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityDataSeeder");

        var tenant = await context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == TenantDefaults.DefaultSlug);

        if (tenant is null)
        {
            tenant = new Tenant
            {
                Name = TenantDefaults.DefaultName,
                Slug = TenantDefaults.DefaultSlug,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
            logger.LogInformation("Created default tenant {Slug}", tenant.Slug);
        }

        foreach (var roleName in RoleNames.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole<int>(roleName));

            if (result.Succeeded)
                logger.LogInformation("Created role {Role}", roleName);
        }

        const string adminEmail = "admin@pharmacy.local";
        const string adminPassword = "Admin@12345";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);

            if (!createResult.Succeeded)
            {
                logger.LogWarning(
                    "Failed to seed admin user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
            logger.LogInformation("Seeded development admin user {Email}", adminEmail);
        }

        var hasMembership = await context.TenantUsers
            .IgnoreQueryFilters()
            .AnyAsync(tu => tu.TenantId == tenant.Id && tu.UserId == adminUser.Id);

        if (!hasMembership)
        {
            context.TenantUsers.Add(new TenantUser
            {
                TenantId = tenant.Id,
                UserId = adminUser.Id,
                Role = RoleNames.Admin
            });

            await context.SaveChangesAsync();
            logger.LogInformation(
                "Linked admin user to tenant {TenantId}",
                tenant.Id);
        }

        await BackfillTenantIdsAsync(context, tenant.Id, logger);
    }

    private static async Task BackfillTenantIdsAsync(
        PharmacyDbContext context,
        int tenantId,
        ILogger logger)
    {
        await context.Categories
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.TenantId, tenantId));

        await context.Medicines
            .IgnoreQueryFilters()
            .Where(m => m.TenantId == 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.TenantId, tenantId));

        await context.Sales
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.TenantId, tenantId));

        await context.SaleItems
            .IgnoreQueryFilters()
            .Where(si => si.TenantId == 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(si => si.TenantId, tenantId));

        logger.LogInformation("Backfilled TenantId {TenantId} on existing business data", tenantId);
    }
}
