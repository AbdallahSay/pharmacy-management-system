using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Application.Common.Interfaces;
using Pharmacy.Application.Medicines.DTOs;
using Pharmacy.Application.Sales.DTOs;
using Pharmacy.Infrastructure.Persistence;
using Pharmacy.IntegrationTests.TestInfrastructure;

namespace Pharmacy.IntegrationTests;

public sealed class StockConcurrencyTests : IClassFixture<PharmacyApiFactory>
{
    private readonly PharmacyApiFactory _factory;
    private readonly AuthTestClient _auth;

    public StockConcurrencyTests(PharmacyApiFactory factory)
    {
        _factory = factory;
        _auth = new AuthTestClient(factory);
    }

    [Fact]
    public async Task Medicine_version_rejects_stale_concurrent_update()
    {
        await _factory.InitializeAsync();

        await using var scope1 = _factory.Services.CreateAsyncScope();
        await using var scope2 = _factory.Services.CreateAsyncScope();

        SetTenant(scope1, _factory.Seed.TenantA.Id);
        SetTenant(scope2, _factory.Seed.TenantA.Id);

        var context1 = scope1.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var context2 = scope2.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        var medicine1 = await context1.Medicines.FirstAsync(
            m => m.Id == _factory.Seed.TenantAMedicineId);

        var medicine2 = await context2.Medicines.FirstAsync(
            m => m.Id == _factory.Seed.TenantAMedicineId);

        medicine1.Stock -= 1;
        await context1.SaveChangesAsync();

        medicine2.Stock -= 1;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context2.SaveChangesAsync());
    }

    [Fact]
    public async Task Second_sale_after_stock_is_depleted_returns_conflict()
    {
        await _factory.InitializeAsync();

        var pharmacist = await _auth.ForUserAsync(
            _factory.Seed.TenantAPharmacist,
            _factory.Seed.TenantA.Slug);

        var tenantAdmin = await _auth.ForUserAsync(
            _factory.Seed.TenantAAdmin,
            _factory.Seed.TenantA.Slug);

        var updateStock = await tenantAdmin.PutAsJsonAsync(
            $"/api/medicines/{_factory.Seed.TenantAMedicineId}",
            new UpdateMedicineDto
            {
                Name = "Tenant A Medicine",
                Price = 10,
                Stock = 1,
                MinStock = 0,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                CategoryId = _factory.Seed.TenantACategoryId
            });

        updateStock.EnsureSuccessStatusCode();

        var saleRequest = new CreateSaleDto
        {
            Items =
            [
                new CreateSaleItemDto
                {
                    MedicineId = _factory.Seed.TenantAMedicineId,
                    Quantity = 1
                }
            ]
        };

        var firstSale = await pharmacist.PostAsJsonAsync("/api/sales", saleRequest);
        var secondSale = await pharmacist.PostAsJsonAsync("/api/sales", saleRequest);

        Assert.Equal(HttpStatusCode.Created, firstSale.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondSale.StatusCode);
    }

    private static void SetTenant(IServiceScope scope, int tenantId)
    {
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetTenant(tenantId);
    }
}
