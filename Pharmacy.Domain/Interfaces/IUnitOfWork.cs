using Pharmacy.Domain.Entities;

namespace Pharmacy.Domain.Interfaces;

public interface IUnitOfWork
{
    IRepository<T> GetRepository<T>() where T : BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
