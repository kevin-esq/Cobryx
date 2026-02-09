using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Interfaces.Lending;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories.Lending;

/// <summary>
/// Repository for managing <see cref="LoanAgreement"/> persistence.
/// </summary>
/// <summary>
/// Repository for managing <see cref="LoanAgreement"/> persistence.
/// </summary>
public class LoanAgreementRepository : ILoanAgreementRepository
{
    private readonly CobryxDbContext _context;

    public LoanAgreementRepository(CobryxDbContext context)
    {
        _context = context;
    }

    public async Task<LoanAgreement?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.LoanAgreements.FindAsync(new object[] { id }, ct);
    }

    public async Task AddAsync(LoanAgreement agreement, CancellationToken ct = default)
    {
        await _context.LoanAgreements.AddAsync(agreement, ct);
    }

    public async Task UpdateAsync(LoanAgreement agreement, CancellationToken ct = default)
    {
        _context.LoanAgreements.Update(agreement);
        await Task.CompletedTask;
    }
}
