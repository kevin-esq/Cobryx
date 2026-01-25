using System.Linq.Expressions;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;

namespace Cobryx.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(Guid id) => Task.FromResult<Payment?>(null);
    public Task<IEnumerable<Payment>> GetAllAsync() => Task.FromResult<IEnumerable<Payment>>(Enumerable.Empty<Payment>());
    public Task<IEnumerable<Payment>> FindAsync(Expression<Func<Payment, bool>> predicate) => Task.FromResult<IEnumerable<Payment>>(Enumerable.Empty<Payment>());
    public Task AddAsync(Payment entity) => Task.CompletedTask;
    public Task UpdateAsync(Payment entity) => Task.CompletedTask;
    public Task DeleteAsync(Guid id) => Task.CompletedTask;

    public Task<IEnumerable<Payment>> GetByCreditAsync(Guid creditId) => Task.FromResult<IEnumerable<Payment>>(Enumerable.Empty<Payment>());
    public Task<IEnumerable<Payment>> GetByTenantAsync(Guid tenantId) => Task.FromResult<IEnumerable<Payment>>(Enumerable.Empty<Payment>());
}
