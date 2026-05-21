using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pharmacy.Application.Common.Constants;
using Pharmacy.Application.Common.Interfaces;
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
        var tenantContext = services.GetRequiredService<ITenantContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityDataSeeder");

        var tenant = await context.Tenants
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

        foreach (var roleName in RoleNames.PlatformRoles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole<int>(roleName));

            if (result.Succeeded)
                logger.LogInformation("Created role {Role}", roleName);
        }

        const string adminEmail = "admin@pharmacy.local";
        const string adminPassword = "Admin@12345";

        ApplicationUser? adminUser;
        using (tenantContext.BeginBypass())
        {
            adminUser = await context.Users
                .FirstOrDefaultAsync(u =>
                    u.TenantId == tenant.Id &&
                    u.NormalizedEmail == userManager.NormalizeEmail(adminEmail));

            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    TenantId = tenant.Id,
                    UserName = BuildTenantUserName(tenant.Id, adminEmail),
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

                await userManager.AddToRoleAsync(adminUser, RoleNames.PlatformAdmin);
                logger.LogInformation("Seeded development platform admin user {Email}", adminEmail);
            }
        }

        using (tenantContext.BeginBypass())
        {
            var membership = await context.TenantUsers
                .FirstOrDefaultAsync(tu => tu.TenantId == tenant.Id && tu.UserId == adminUser.Id);

            if (membership is null)
            {
                context.TenantUsers.Add(new TenantUser
                {
                    TenantId = tenant.Id,
                    UserId = adminUser.Id,
                    Role = RoleNames.PlatformAdmin
                });

                await context.SaveChangesAsync();
                logger.LogInformation(
                    "Linked admin user to tenant {TenantId}",
                    tenant.Id);
            }
            else if (membership.Role != RoleNames.PlatformAdmin)
            {
                membership.Role = RoleNames.PlatformAdmin;
                await context.SaveChangesAsync();
                logger.LogInformation(
                    "Updated seeded admin membership role to {Role}",
                    RoleNames.PlatformAdmin);
            }
        }

        using (tenantContext.BeginBypass())
        {
            await BackfillTenantIdsAsync(context, tenant.Id, logger);
        }
    }

    private static async Task BackfillTenantIdsAsync(
        PharmacyDbContext context,
        int tenantId,
        ILogger logger)
    {
        await context.Categories
            .Where(c => c.TenantId == 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.TenantId, tenantId));

        await context.Medicines
            .Where(m => m.TenantId == 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.TenantId, tenantId));

        await context.Sales
            .Where(s => s.TenantId == 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.TenantId, tenantId));

        await context.SaleItems
            .Where(si => si.TenantId == 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(si => si.TenantId, tenantId));

        logger.LogInformation("Backfilled TenantId {TenantId} on existing business data", tenantId);
    }

    private static string BuildTenantUserName(int tenantId, string email)
    {
        var normalized = new string(email.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        return $"tenant{tenantId}{normalized}";
    }
}
