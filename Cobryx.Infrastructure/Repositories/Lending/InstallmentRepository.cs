using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Interfaces.Lending;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories.Lending;

/// <summary>
/// Repository for managing <see cref="Installment"/> persistence.
/// </summary>
public class InstallmentRepository : IInstallmentRepository
{
    private readonly CobryxDbContext _context;

    public InstallmentRepository(CobryxDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Installment>> GetByLoanIdAsync(Guid loanId, CancellationToken ct = default)
    {
        return await _context.LoanInstallments
            .Where(i => i.LoanId == loanId)
            .OrderBy(i => i.InstallmentNumber)
            .ToListAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<Installment> installments, CancellationToken ct = default)
    {
        await _context.LoanInstallments.AddRangeAsync(installments, ct);
    }

    public async Task UpdateAsync(Installment installment, CancellationToken ct = default)
    {
        _context.LoanInstallments.Update(installment);
        await Task.CompletedTask;
    }

    public async Task UpdateRangeAsync(IEnumerable<Installment> installments, CancellationToken ct = default)
    {
        _context.LoanInstallments.UpdateRange(installments);
        await Task.CompletedTask;
    }
}
