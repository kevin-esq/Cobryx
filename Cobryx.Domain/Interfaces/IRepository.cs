using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity, IAggregateRoot
{
    IQueryable<T> Query();
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
