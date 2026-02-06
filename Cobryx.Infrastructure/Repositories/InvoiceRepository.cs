using Cobryx.Domain.Entities.Invoicing;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class InvoiceRepository : BaseRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(CobryxDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Invoice?> GetByNumberAsync(Guid tenantId, string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetByCustomerAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Include(i => i.Items)
            .Where(i => i.TenantId == tenantId && i.CustomerId == customerId)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public override async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }
}
