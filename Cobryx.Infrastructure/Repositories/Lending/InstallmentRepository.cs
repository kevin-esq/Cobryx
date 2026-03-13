using Cobryx.Domain.Lending;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories.Lending;

/// <summary>
/// Repository for managing <see cref="Installment"/> persistence.
/// </summary>
public class InstallmentRepository(CobryxDbContext context) : IInstallmentRepository
{
    private readonly CobryxDbContext _context = context;

    public async Task<IReadOnlyList<Installment>> GetByLoanIdAsync(Guid loanId, CancellationToken ct = default)
    {
        return await _context.Installments
            .Where(i => i.LoanId == loanId)
            .OrderBy(i => i.InstallmentNumber)
            .ToListAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<Installment> installments, CancellationToken ct = default) => await _context.Installments.AddRangeAsync(installments, ct);

    public async Task UpdateAsync(Installment installment, CancellationToken ct = default)
    {
        _context.Installments.Update(installment);
        await Task.CompletedTask;
    }

    public async Task UpdateRangeAsync(IEnumerable<Installment> installments, CancellationToken ct = default)
    {
        _context.Installments.UpdateRange(installments);
        await Task.CompletedTask;
    }
}
