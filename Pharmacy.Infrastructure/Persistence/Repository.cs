using Microsoft.EntityFrameworkCore;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Entities;
using Pharmacy.Domain.Interfaces;

namespace Pharmacy.Infrastructure.Persistence;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly PharmacyDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(PharmacyDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<IReadOnlyList<T>> ListAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(skip, take);

        return await _dbSet
            .AsNoTracking()
            .OrderBy(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<T?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbSet
            .AsNoTracking()
            .AnyAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id != 0)
            throw new ArgumentException("Cannot insert an entity with a pre-set Id.", nameof(entity));

        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await _dbSet
            .Where(e => e.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return rowsAffected > 0;
    }

    private static void ValidatePagination(int skip, int take)
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be non-negative.");

        if (take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be greater than zero.");

        if (take > RepositoryLimits.MaxPageSize)
            throw new ArgumentOutOfRangeException(
                nameof(take),
                $"Take cannot exceed {RepositoryLimits.MaxPageSize}.");
    }
}
