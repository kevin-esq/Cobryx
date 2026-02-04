using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class SupportTicketRepository : ISupportTicketRepository
{
    private readonly CobryxDbContext _context;

    public SupportTicketRepository(CobryxDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        await _context.SupportTickets.AddAsync(ticket, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SupportTickets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
