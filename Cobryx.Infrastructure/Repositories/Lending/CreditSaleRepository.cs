using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Interfaces.Lending;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories.Lending;

/// <summary>
/// Repository for managing <see cref="CreditSale"/> persistence.
/// </summary>
public class CreditSaleRepository : ICreditSaleRepository
{
    private readonly CobryxDbContext _context;

    public CreditSaleRepository(CobryxDbContext context)
    {
        _context = context;
    }

    public async Task<CreditSale?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.CreditSales.FindAsync(new object[] { id }, ct);
    }

    public async Task<IReadOnlyList<CreditSale>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        return await _context.CreditSales
            .Where(cs => cs.CustomerId == customerId)
            .OrderByDescending(cs => cs.SaleDate)
            .ToListAsync(ct);
    }

    public async Task AddAsync(CreditSale creditSale, CancellationToken ct = default)
    {
        await _context.CreditSales.AddAsync(creditSale, ct);
    }

    public async Task UpdateAsync(CreditSale creditSale, CancellationToken ct = default)
    {
        _context.CreditSales.Update(creditSale);
        await Task.CompletedTask;
    }
}
