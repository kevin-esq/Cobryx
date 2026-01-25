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

    public virtual async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

    public virtual async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => 
        await _dbSet.Where(predicate).ToListAsync();

    public virtual async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(T entity)
    {
        _dbContext.Entry(entity).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}
