namespace Pharmacy.Application.Tenants.Contracts;

public sealed record TenantListItemDto(
    int Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAt);