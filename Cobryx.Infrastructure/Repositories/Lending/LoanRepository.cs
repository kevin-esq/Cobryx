using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Interfaces.Lending;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories.Lending;

/// <summary>
/// Repository for managing <see cref="Loan"/> persistence.
/// </summary>
public class LoanRepository : ILoanRepository
{
    private readonly CobryxDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoanRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public LoanRepository(CobryxDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves a loan by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the loan.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The loan if found, otherwise null.</returns>
    public async Task<Loan?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Loans.FindAsync(new object[] { id }, ct);
    }

    /// <summary>
    /// Retrieves a loan by its unique identifier, including its installments.
    /// </summary>
    /// <param name="id">The unique identifier of the loan.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The loan with its installments if found, otherwise null.</returns>
    public async Task<Loan?> GetByIdWithInstallmentsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Loans
            .Include(l => l.Installments.OrderBy(i => i.InstallmentNumber))
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    /// <summary>
    /// Retrieves a read-only list of loans associated with a specific customer.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A read-only list of loans for the specified customer.</returns>
    public async Task<IReadOnlyList<Loan>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        return await _context.Loans
            .Where(l => l.CustomerId == customerId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves a read-only list of active loans for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A read-only list of active loans for the specified tenant.</returns>
    public async Task<IReadOnlyList<Loan>> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _context.Loans
            .Where(l => l.TenantId == tenantId && l.Status == Domain.Entities.Lending.Enums.LoanStatus.Active)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Loan loan, CancellationToken ct = default)
    {
        await _context.Loans.AddAsync(loan, ct);
    }

    public async Task UpdateAsync(Loan loan, CancellationToken ct = default)
    {
        _context.Loans.Update(loan);
        await Task.CompletedTask;
    }
}
