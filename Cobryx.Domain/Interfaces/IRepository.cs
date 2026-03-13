using System.Linq.Expressions;

using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity, IAggregateRoot
{
    public IQueryable<T> Query();
    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    public Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    public Task AddAsync(T entity, CancellationToken cancellationToken = default);
    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
