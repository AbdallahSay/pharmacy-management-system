using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Tenants.Contracts;
using Pharmacy.Application.Tenants.DTOs;

namespace Pharmacy.Application.Tenants.Interfaces;

public interface ITenantService
{
    /// <summary>
    /// Creates a new pharmacy tenant and its first TenantAdmin user in one atomic operation.
    /// </summary>
    Task<TenantCreatedResponse> CreateAsync(
        CreateTenantDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of all tenants (super-admin view).
    /// </summary>
    Task<PagedResponse<TenantListItemDto>> GetPagedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
