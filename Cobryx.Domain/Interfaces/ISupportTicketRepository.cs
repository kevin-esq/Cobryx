using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface ISupportTicketRepository
{
    Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
