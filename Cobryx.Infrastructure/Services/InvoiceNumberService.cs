using Cobryx.Application.Common.Interfaces;
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
        // Simple but robust implementation: count existing invoices for the tenant
        // In a real production environment with high concurrency, we might use a dedicated sequence table or Redis
        var count = await _dbContext.Invoices
            .IgnoreQueryFilters() // Ensure we see all invoices for numbering
            .Where(i => i.TenantId == tenantId)
            .CountAsync();

        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }
}
