using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface ISupportTicketRepository
{
    Task AddAsync(SupportTicket ticket);
    Task<SupportTicket?> GetByIdAsync(Guid id);
}
