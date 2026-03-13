using Cobryx.Domain.Accounting;


namespace Cobryx.Domain.Interfaces;

public interface IInvoiceRepository : IRepository<Invoice>
{
    public Task<Invoice?> GetByNumberAsync(Guid tenantId, string invoiceNumber, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<Invoice>> GetByCustomerAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
}
