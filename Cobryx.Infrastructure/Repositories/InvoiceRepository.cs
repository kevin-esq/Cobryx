using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class InvoiceRepository : BaseRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(CobryxDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Invoice?> GetByNumberAsync(Guid tenantId, string invoiceNumber)
    {
        return await _dbContext.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.InvoiceNumber == invoiceNumber);
    }

    public async Task<IReadOnlyList<Invoice>> GetByCustomerAsync(Guid tenantId, Guid customerId)
    {
        return await _dbContext.Invoices
            .Include(i => i.Items)
            .Where(i => i.TenantId == tenantId && i.CustomerId == customerId)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();
    }

    public override async Task<Invoice?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);
    }
}
