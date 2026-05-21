namespace Pharmacy.Application.Tenants.Contracts;

/// <summary>
/// Returned after successfully creating a new tenant + its first TenantAdmin user.
/// </summary>
public sealed record TenantCreatedResponse(
    int TenantId,
    string PharmacyName,
    string Slug,
    int AdminUserId,
    string AdminEmail,
    DateTime CreatedAt);
