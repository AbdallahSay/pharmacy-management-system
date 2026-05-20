using Pharmacy.Domain.Entities;

namespace Pharmacy.Application.Sales.Interfaces;

/// <summary>
/// Read-optimized sale queries. Implemented in Infrastructure (EF Core).
/// </summary>
public interface ISaleReadRepository
{
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sale>> GetPagedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaleItem>> GetItemsBySaleIdAsync(
        int saleId,
        CancellationToken cancellationToken = default);
}
