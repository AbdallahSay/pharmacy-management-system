using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pharmacy.API;
using Pharmacy.Application.Common.Constants;
using Pharmacy.Application.Common.Interfaces;
using Pharmacy.Domain.Entities;
using Pharmacy.Infrastructure.Auth;
using Pharmacy.Infrastructure.Persistence;
using Pharmacy.Infrastructure.Persistence.Interceptors;

namespace Pharmacy.IntegrationTests.TestInfrastructure;

public sealed class PharmacyApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtKey = "integration-test-jwt-signing-key-with-enough-length";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ServiceProvider _sqliteServices = new ServiceCollection()
        .AddEntityFrameworkSqlite()
        .BuildServiceProvider();
    private bool _initialized;

    public TestSeedData Seed { get; private set; } = null!;

    public PharmacyApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Issuer", "PharmacyApi");
        Environment.SetEnvironmentVariable("Jwt__Audience", "PharmacyClients");
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("Jwt__ExpiresInMinutes", "60");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Data Source=:memory:");
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _ = CreateClient();

        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");

        Seed = await SeedAsync(scope.ServiceProvider);
        _initialized = true;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("Jwt__Key", null);
        Environment.SetEnvironmentVariable("Jwt__ExpiresInMinutes", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        await _sqliteServices.DisposeAsync();
        await _connection.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "PharmacyApi",
                ["Jwt:Audience"] = "PharmacyClients",
                ["Jwt:Key"] = JwtKey,
                ["Jwt:ExpiresInMinutes"] = "60",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                ["Logging:LogLevel:Default"] = "Warning",
                ["AllowedHosts"] = "*"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<PharmacyDbContext>>();
            services.RemoveAll<DbConnection>();
            RemoveDbContextOptionsConfigurations(services);

            _connection.Open();
            services.AddSingleton<DbConnection>(_connection);

            services.AddDbContext<PharmacyDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(_connection);
                options.UseInternalServiceProvider(_sqliteServices);
                options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
            });
        });
    }

    private static void RemoveDbContextOptionsConfigurations(IServiceCollection services)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            var serviceType = services[i].ServiceType;

            if (serviceType.IsGenericType &&
                serviceType.GetGenericTypeDefinition().FullName == "Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration`1" &&
                serviceType.GenericTypeArguments[0] == typeof(PharmacyDbContext))
            {
                services.RemoveAt(i);
            }
        }
    }

    private static async Task<TestSeedData> SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<PharmacyDbContext>();
        var tenantContext = services.GetRequiredService<ITenantContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

        foreach (var role in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<int>(role));
        }

        var tenantA = new Tenant { Name = "Tenant A", Slug = "tenant-a", IsActive = true };
        var tenantB = new Tenant { Name = "Tenant B", Slug = "tenant-b", IsActive = true };

        context.Tenants.AddRange(tenantA, tenantB);
        await context.SaveChangesAsync();

        using var bypass = tenantContext.BeginBypass();

        var platformAdmin = await CreateUserAsync(
            context,
            userManager,
            tenantA.Id,
            "platform@tenant-a.test",
            RoleNames.PlatformAdmin);

        await userManager.AddToRoleAsync(
            await context.Users.FirstAsync(user => user.Id == platformAdmin.Id),
            RoleNames.PlatformAdmin);

        var tenantAAdmin = await CreateUserAsync(
            context,
            userManager,
            tenantA.Id,
            "admin@tenant-a.test",
            RoleNames.TenantAdmin);

        var tenantAPharmacist = await CreateUserAsync(
            context,
            userManager,
            tenantA.Id,
            "pharmacist@tenant-a.test",
            RoleNames.Pharmacist);

        var tenantBAdmin = await CreateUserAsync(
            context,
            userManager,
            tenantB.Id,
            "admin@tenant-b.test",
            RoleNames.TenantAdmin);

        var tenantACategory = new Category { TenantId = tenantA.Id, Name = "Tenant A Category" };
        var tenantBCategory = new Category { TenantId = tenantB.Id, Name = "Tenant B Category" };
        context.Categories.AddRange(tenantACategory, tenantBCategory);
        await context.SaveChangesAsync();

        var tenantAMedicine = new Medicine
        {
            TenantId = tenantA.Id,
            CategoryId = tenantACategory.Id,
            Name = "Tenant A Medicine",
            Price = 10,
            Stock = 20,
            MinStock = 1,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            IsActive = true
        };

        var tenantBMedicine = new Medicine
        {
            TenantId = tenantB.Id,
            CategoryId = tenantBCategory.Id,
            Name = "Tenant B Medicine",
            Price = 15,
            Stock = 30,
            MinStock = 1,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            IsActive = true
        };

        context.Medicines.AddRange(tenantAMedicine, tenantBMedicine);
        await context.SaveChangesAsync();

        var tenantASale = new Sale
        {
            TenantId = tenantA.Id,
            UserId = tenantAAdmin.Id,
            SaleDate = DateTime.UtcNow,
            TotalAmount = tenantAMedicine.Price
        };

        var tenantBSale = new Sale
        {
            TenantId = tenantB.Id,
            UserId = tenantBAdmin.Id,
            SaleDate = DateTime.UtcNow,
            TotalAmount = tenantBMedicine.Price
        };

        context.Sales.AddRange(tenantASale, tenantBSale);
        await context.SaveChangesAsync();

        context.SaleItems.AddRange(
            new SaleItem
            {
                TenantId = tenantA.Id,
                SaleId = tenantASale.Id,
                MedicineId = tenantAMedicine.Id,
                Quantity = 1,
                UnitPrice = tenantAMedicine.Price
            },
            new SaleItem
            {
                TenantId = tenantB.Id,
                SaleId = tenantBSale.Id,
                MedicineId = tenantBMedicine.Id,
                Quantity = 1,
                UnitPrice = tenantBMedicine.Price
            });

        await context.SaveChangesAsync();

        return new TestSeedData(
            new TestTenant(tenantA.Id, tenantA.Name, tenantA.Slug),
            new TestTenant(tenantB.Id, tenantB.Name, tenantB.Slug),
            platformAdmin,
            tenantAAdmin,
            tenantAPharmacist,
            tenantBAdmin,
            tenantACategory.Id,
            tenantBCategory.Id,
            tenantAMedicine.Id,
            tenantBMedicine.Id,
            tenantASale.Id,
            tenantBSale.Id);
    }

    private static async Task<TestUser> CreateUserAsync(
        PharmacyDbContext context,
        UserManager<ApplicationUser> userManager,
        int tenantId,
        string email,
        string role)
    {
        const string password = "Password1";
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = new ApplicationUser
        {
            TenantId = tenantId,
            UserName = BuildTenantUserName(tenantId, normalizedEmail),
            Email = normalizedEmail,
            FullName = $"{role} User",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(error => error.Description)));

        context.TenantUsers.Add(new TenantUser
        {
            TenantId = tenantId,
            UserId = user.Id,
            Role = role
        });

        await context.SaveChangesAsync();

        return new TestUser(user.Id, normalizedEmail, password, role);
    }

    private static string BuildTenantUserName(int tenantId, string email)
    {
        var normalized = new string(email.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        return $"tenant{tenantId}{normalized}";
    }
}
