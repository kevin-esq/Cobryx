using Cobryx.Domain.Identity;

namespace Cobryx.Domain.Interfaces;

public interface ISupportTicketRepository
{
    public Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    public Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
