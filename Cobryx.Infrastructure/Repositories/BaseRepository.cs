using System.Linq.Expressions;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public abstract class BaseRepository<T> : IRepository<T>
    where T : BaseEntity, IAggregateRoot
{
    protected readonly CobryxDbContext _dbContext;
    protected readonly DbSet<T> _dbSet;

    protected BaseRepository(CobryxDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = dbContext.Set<T>();
    }

    public virtual IQueryable<T> Query() => _dbSet;

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbSet.FindAsync(new object[] { id }, cancellationToken);

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbSet.ToListAsync(cancellationToken);

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _dbSet.Where(predicate).ToListAsync(cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }
}
