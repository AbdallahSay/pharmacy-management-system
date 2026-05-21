namespace Pharmacy.IntegrationTests.TestInfrastructure;

public sealed record TestUser(
    int Id,
    string Email,
    string Password,
    string Role);

public sealed record TestTenant(
    int Id,
    string Name,
    string Slug);

public sealed record TestSeedData(
    TestTenant TenantA,
    TestTenant TenantB,
    TestUser PlatformAdmin,
    TestUser TenantAAdmin,
    TestUser TenantAPharmacist,
    TestUser TenantBAdmin,
    int TenantACategoryId,
    int TenantBCategoryId,
    int TenantAMedicineId,
    int TenantBMedicineId,
    int TenantASaleId,
    int TenantBSaleId);
