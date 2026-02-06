using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Services;

public class InvoiceNumberService : IInvoiceNumberService
{
    private readonly CobryxDbContext _dbContext;

    public InvoiceNumberService(CobryxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateNextNumberAsync(Guid tenantId)
    {
        var count = await _dbContext.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.TenantId == tenantId)
            .CountAsync();

        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }
}
