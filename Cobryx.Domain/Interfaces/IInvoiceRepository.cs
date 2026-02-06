using Cobryx.Domain.Entities.Invoicing;
using Cobryx.Domain.Interfaces;

namespace Cobryx.Domain.Interfaces;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<Invoice?> GetByNumberAsync(Guid tenantId, string invoiceNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetByCustomerAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
}
