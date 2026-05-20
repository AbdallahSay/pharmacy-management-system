using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Sales.Interfaces;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Entities;

namespace Pharmacy.Infrastructure.Persistence;

public sealed class SaleReadRepository : ISaleReadRepository
{
    private readonly PharmacyDbContext _context;

    public SaleReadRepository(PharmacyDbContext context)
    {
        _context = context;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return _context.Sales.AsNoTracking().CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sale>> GetPagedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(skip, take);

        return await _context.Sales
            .AsNoTracking()
            .OrderByDescending(s => s.SaleDate)
            .ThenByDescending(s => s.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SaleItem>> GetItemsBySaleIdAsync(
        int saleId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SaleItems
            .AsNoTracking()
            .Where(si => si.SaleId == saleId)
            .OrderBy(si => si.Id)
            .ToListAsync(cancellationToken);
    }

    private static void ValidatePagination(int skip, int take)
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip));

        if (take <= 0 || take > RepositoryLimits.MaxPageSize)
            throw new ArgumentOutOfRangeException(nameof(take));
    }
}
