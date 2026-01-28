using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;

namespace Cobryx.Application.Common.Interfaces;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<Invoice?> GetByNumberAsync(Guid tenantId, string invoiceNumber);
    Task<IReadOnlyList<Invoice>> GetByCustomerAsync(Guid tenantId, Guid customerId);
}
