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

    public async Task AddAsync(SupportTicket ticket)
    {
        await _context.SupportTickets.AddAsync(ticket);
        await _context.SaveChangesAsync();
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id)
    {
        return await _context.SupportTickets.FirstOrDefaultAsync(x => x.Id == id);
    }
}
