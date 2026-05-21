using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Application.Auth.DTOs;
using Pharmacy.Application.Categories.DTOs;
using Pharmacy.Application.Common.Constants;
using Pharmacy.Application.Common.Interfaces;
using Pharmacy.Application.Medicines.DTOs;
using Pharmacy.Application.Sales.DTOs;
using Pharmacy.Domain.Entities;
using Pharmacy.Infrastructure.Persistence;
using Pharmacy.IntegrationTests.TestInfrastructure;

namespace Pharmacy.IntegrationTests;

public sealed class MultiTenantSecurityTests : IClassFixture<PharmacyApiFactory>
{
    private readonly PharmacyApiFactory _factory;
    private readonly AuthTestClient _auth;

    public MultiTenantSecurityTests(PharmacyApiFactory factory)
    {
        _factory = factory;
        _auth = new AuthTestClient(factory);
    }

    [Fact]
    public async Task TenantA_cannot_list_tenantB_data()
    {
        await _factory.InitializeAsync();
        var client = await _auth.ForUserAsync(_factory.Seed.TenantAAdmin, _factory.Seed.TenantA.Slug);

        var categories = await client.GetAsync("/api/categories?skip=0&take=50");
        var medicines = await client.GetAsync("/api/medicines?skip=0&take=50");
        var sales = await client.GetAsync("/api/sales?skip=0&take=50");

        categories.EnsureSuccessStatusCode();
        medicines.EnsureSuccessStatusCode();
        sales.EnsureSuccessStatusCode();

        Assert.True(await JsonAssertions.PagedItemsContainIdAsync(categories, _factory.Seed.TenantACategoryId));
        Assert.False(await JsonAssertions.PagedItemsContainIdAsync(categories, _factory.Seed.TenantBCategoryId));
        Assert.True(await JsonAssertions.PagedItemsContainIdAsync(medicines, _factory.Seed.TenantAMedicineId));
        Assert.False(await JsonAssertions.PagedItemsContainIdAsync(medicines, _factory.Seed.TenantBMedicineId));
        Assert.True(await JsonAssertions.PagedItemsContainIdAsync(sales, _factory.Seed.TenantASaleId));
        Assert.False(await JsonAssertions.PagedItemsContainIdAsync(sales, _factory.Seed.TenantBSaleId));
    }

    [Fact]
    public async Task TenantA_cannot_get_tenantB_entities_by_id()
    {
        await _factory.InitializeAsync();
        var client = await _auth.ForUserAsync(_factory.Seed.TenantAAdmin, _factory.Seed.TenantA.Slug);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/categories/{_factory.Seed.TenantBCategoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/medicines/{_factory.Seed.TenantBMedicineId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/sales/{_factory.Seed.TenantBSaleId}")).StatusCode);
    }

    [Fact]
    public async Task TenantA_cannot_update_or_delete_tenantB_data()
    {
        await _factory.InitializeAsync();
        var client = await _auth.ForUserAsync(_factory.Seed.TenantAAdmin, _factory.Seed.TenantA.Slug);

        var updateCategory = await client.PutAsJsonAsync(
            $"/api/categories/{_factory.Seed.TenantBCategoryId}",
            new UpdateCategoryDto { Name = "Cross Tenant Update" });

        var deleteCategory = await client.DeleteAsync($"/api/categories/{_factory.Seed.TenantBCategoryId}");
        var deleteMedicine = await client.DeleteAsync($"/api/medicines/{_factory.Seed.TenantBMedicineId}");

        Assert.Equal(HttpStatusCode.NotFound, updateCategory.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteCategory.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteMedicine.StatusCode);
    }

    [Fact]
    public async Task TenantA_cannot_reference_tenantB_entities()
    {
        await _factory.InitializeAsync();
        var client = await _auth.ForUserAsync(_factory.Seed.TenantAAdmin, _factory.Seed.TenantA.Slug);

        var createMedicine = await client.PostAsJsonAsync("/api/medicines", new CreateMedicineDto
        {
            Name = "Cross Tenant Medicine",
            CategoryId = _factory.Seed.TenantBCategoryId,
            Price = 20,
            Stock = 5
        });

        var createSale = await client.PostAsJsonAsync("/api/sales", new CreateSaleDto
        {
            Items =
            [
                new CreateSaleItemDto
                {
                    MedicineId = _factory.Seed.TenantBMedicineId,
                    Quantity = 1
                }
            ]
        });

        Assert.Equal(HttpStatusCode.NotFound, createMedicine.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, createSale.StatusCode);
    }

    [Fact]
    public async Task User_cannot_authenticate_into_another_tenant()
    {
        await _factory.InitializeAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = _factory.Seed.TenantAPharmacist.Email,
            Password = _factory.Seed.TenantAPharmacist.Password,
            TenantSlug = _factory.Seed.TenantB.Slug
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TenantUser_and_identity_queries_fail_closed_without_tenant_context()
    {
        await _factory.InitializeAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        Assert.Equal(0, await context.Categories.CountAsync());
        Assert.Equal(0, await context.Medicines.CountAsync());
        Assert.Equal(0, await context.Sales.CountAsync());
        Assert.Equal(0, await context.SaleItems.CountAsync());
        Assert.Equal(0, await context.TenantUsers.CountAsync());
        Assert.Equal(0, await context.Users.CountAsync());
    }

    [Fact]
    public async Task Composite_foreign_keys_reject_cross_tenant_references()
    {
        await _factory.InitializeAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        using var bypass = tenantContext.BeginBypass();

        context.Medicines.Add(new Medicine
        {
            TenantId = _factory.Seed.TenantA.Id,
            CategoryId = _factory.Seed.TenantBCategoryId,
            Name = "Invalid Cross Tenant FK",
            Price = 1,
            Stock = 1,
            MinStock = 0,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            IsActive = true
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Jwt_tenant_claim_is_enforced()
    {
        await _factory.InitializeAsync();
        var client = _auth.WithForgedToken(
            _factory.Seed.TenantAAdmin.Id,
            _factory.Seed.TenantB.Id,
            RoleNames.TenantAdmin);

        var response = await client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Tenant_role_claim_must_match_membership_role()
    {
        await _factory.InitializeAsync();
        var client = _auth.WithForgedToken(
            _factory.Seed.TenantAPharmacist.Id,
            _factory.Seed.TenantA.Id,
            RoleNames.TenantAdmin);

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = "illegal-admin-create@tenant-a.test",
            Password = "Password1",
            ConfirmPassword = "Password1",
            FullName = "Illegal Admin Create",
            Role = RoleNames.Pharmacist
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PlatformAdmin_and_TenantAdmin_boundaries_are_enforced()
    {
        await _factory.InitializeAsync();
        var tenantAdmin = await _auth.ForUserAsync(_factory.Seed.TenantAAdmin, _factory.Seed.TenantA.Slug);
        var platformAdmin = await _auth.ForUserAsync(_factory.Seed.PlatformAdmin, _factory.Seed.TenantA.Slug);
        var pharmacist = await _auth.ForUserAsync(_factory.Seed.TenantAPharmacist, _factory.Seed.TenantA.Slug);

        var tenantCreateAttempt = await tenantAdmin.GetAsync("/api/tenants");
        var platformList = await platformAdmin.GetAsync("/api/tenants");
        var pharmacistRegister = await pharmacist.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = "pharmacist-create@tenant-a.test",
            Password = "Password1",
            ConfirmPassword = "Password1",
            FullName = "Unauthorized Create",
            Role = RoleNames.Pharmacist
        });

        Assert.Equal(HttpStatusCode.Forbidden, tenantCreateAttempt.StatusCode);
        Assert.Equal(HttpStatusCode.OK, platformList.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, pharmacistRegister.StatusCode);
    }

    [Fact]
    public async Task TenantAdmin_can_manage_users_only_inside_current_tenant()
    {
        await _factory.InitializeAsync();
        var tenantAAdmin = await _auth.ForUserAsync(_factory.Seed.TenantAAdmin, _factory.Seed.TenantA.Slug);

        var response = await tenantAAdmin.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = "new-user@tenant-a.test",
            Password = "Password1",
            ConfirmPassword = "Password1",
            FullName = "New Tenant A User",
            Role = RoleNames.Pharmacist
        });

        response.EnsureSuccessStatusCode();
        var loginAsTenantA = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = "new-user@tenant-a.test",
            Password = "Password1",
            TenantSlug = _factory.Seed.TenantA.Slug
        });
        var loginAsTenantB = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = "new-user@tenant-a.test",
            Password = "Password1",
            TenantSlug = _factory.Seed.TenantB.Slug
        });

        Assert.Equal(HttpStatusCode.OK, loginAsTenantA.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginAsTenantB.StatusCode);
    }
}
