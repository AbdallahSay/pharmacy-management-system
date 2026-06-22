using System.Net;
using System.Net.Http.Json;
using Pharmacy.Application.Categories.Contracts;
using Pharmacy.Application.Categories.DTOs;
using Pharmacy.Application.Medicines.Contracts;
using Pharmacy.Application.Medicines.DTOs;
using Pharmacy.Application.Sales.DTOs;
using Pharmacy.IntegrationTests.TestInfrastructure;

namespace Pharmacy.IntegrationTests;

public sealed class RolePermissionTests : IClassFixture<PharmacyApiFactory>
{
    private readonly PharmacyApiFactory _factory;
    private readonly AuthTestClient _auth;

    public RolePermissionTests(PharmacyApiFactory factory)
    {
        _factory = factory;
        _auth = new AuthTestClient(factory);
    }

    [Fact]
    public async Task Pharmacist_can_read_medicines_categories_and_sales()
    {
        await _factory.InitializeAsync();
        var pharmacist = await _auth.ForUserAsync(
            _factory.Seed.TenantAPharmacist,
            _factory.Seed.TenantA.Slug);

        var medicines = await pharmacist.GetAsync("/api/medicines?skip=0&take=20");
        var categories = await pharmacist.GetAsync("/api/categories?skip=0&take=20");
        var sales = await pharmacist.GetAsync("/api/sales?skip=0&take=20");
        var medicine = await pharmacist.GetAsync($"/api/medicines/{_factory.Seed.TenantAMedicineId}");
        var category = await pharmacist.GetAsync($"/api/categories/{_factory.Seed.TenantACategoryId}");
        var sale = await pharmacist.GetAsync($"/api/sales/{_factory.Seed.TenantASaleId}");

        Assert.Equal(HttpStatusCode.OK, medicines.StatusCode);
        Assert.Equal(HttpStatusCode.OK, categories.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sales.StatusCode);
        Assert.Equal(HttpStatusCode.OK, medicine.StatusCode);
        Assert.Equal(HttpStatusCode.OK, category.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sale.StatusCode);
    }

    [Fact]
    public async Task Pharmacist_cannot_mutate_medicines_or_categories()
    {
        await _factory.InitializeAsync();
        var pharmacist = await _auth.ForUserAsync(
            _factory.Seed.TenantAPharmacist,
            _factory.Seed.TenantA.Slug);

        var createMedicine = await pharmacist.PostAsJsonAsync("/api/medicines", new CreateMedicineDto
        {
            Name = "Pharmacist Medicine",
            CategoryId = _factory.Seed.TenantACategoryId,
            Price = 10,
            Stock = 5
        });

        var updateMedicine = await pharmacist.PutAsJsonAsync(
            $"/api/medicines/{_factory.Seed.TenantAMedicineId}",
            new UpdateMedicineDto
            {
                Name = "Pharmacist Update",
                Price = 10,
                Stock = 5,
                MinStock = 0,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                CategoryId = _factory.Seed.TenantACategoryId
            });

        var deleteMedicine = await pharmacist.DeleteAsync($"/api/medicines/{_factory.Seed.TenantAMedicineId}");

        var createCategory = await pharmacist.PostAsJsonAsync("/api/categories", new CreateCategoryDto
        {
            Name = "Pharmacist Category"
        });

        var updateCategory = await pharmacist.PutAsJsonAsync(
            $"/api/categories/{_factory.Seed.TenantACategoryId}",
            new UpdateCategoryDto { Name = "Pharmacist Category Update" });

        var deleteCategory = await pharmacist.DeleteAsync($"/api/categories/{_factory.Seed.TenantACategoryId}");

        Assert.Equal(HttpStatusCode.Forbidden, createMedicine.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateMedicine.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteMedicine.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createCategory.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateCategory.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteCategory.StatusCode);
    }

    [Fact]
    public async Task Pharmacist_can_create_sales()
    {
        await _factory.InitializeAsync();
        var pharmacist = await _auth.ForUserAsync(
            _factory.Seed.TenantAPharmacist,
            _factory.Seed.TenantA.Slug);

        var response = await pharmacist.PostAsJsonAsync("/api/sales", new CreateSaleDto
        {
            Items =
            [
                new CreateSaleItemDto
                {
                    MedicineId = _factory.Seed.TenantAMedicineId,
                    Quantity = 1
                }
            ]
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task TenantAdmin_can_mutate_medicines_and_categories()
    {
        await _factory.InitializeAsync();
        var tenantAdmin = await _auth.ForUserAsync(
            _factory.Seed.TenantAAdmin,
            _factory.Seed.TenantA.Slug);

        var createCategory = await tenantAdmin.PostAsJsonAsync("/api/categories", new CreateCategoryDto
        {
            Name = "Admin Category"
        });

        createCategory.EnsureSuccessStatusCode();
        var createdCategory = await createCategory.Content.ReadFromJsonAsync<CreateCategoryResponse>()
            ?? throw new InvalidOperationException("Create category did not return a response body.");

        var createMedicine = await tenantAdmin.PostAsJsonAsync("/api/medicines", new CreateMedicineDto
        {
            Name = "Admin Medicine",
            CategoryId = createdCategory.Id,
            Price = 12,
            Stock = 8
        });

        createMedicine.EnsureSuccessStatusCode();
        var createdMedicine = await createMedicine.Content.ReadFromJsonAsync<CreateMedicineResponse>()
            ?? throw new InvalidOperationException("Create medicine did not return a response body.");

        var updateMedicine = await tenantAdmin.PutAsJsonAsync(
            $"/api/medicines/{createdMedicine.Id}",
            new UpdateMedicineDto
            {
                Name = "Admin Medicine Updated",
                Price = 12,
                Stock = 8,
                MinStock = 1,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                CategoryId = createdCategory.Id
            });

        var deleteMedicine = await tenantAdmin.DeleteAsync($"/api/medicines/{createdMedicine.Id}");
        var deleteCategory = await tenantAdmin.DeleteAsync($"/api/categories/{createdCategory.Id}");

        Assert.Equal(HttpStatusCode.NoContent, updateMedicine.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteMedicine.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteCategory.StatusCode);
    }
}
