using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Accounting.Services;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Lending.Services;

public class LoanPaymentService
{
    private readonly ICobryxDbContext _context;
    private readonly IPaymentAllocationEngine _allocationEngine;
    private readonly ILoanAccrualEngine _accrualEngine;
    private readonly FinancialPostingEngine _postingEngine;
    private readonly FinancialStateEngine _stateEngine;
    private readonly ILogger<LoanPaymentService> _logger;

    public LoanPaymentService(
        ICobryxDbContext context,
        IPaymentAllocationEngine allocationEngine,
        ILoanAccrualEngine accrualEngine,
        FinancialPostingEngine postingEngine,
        FinancialStateEngine stateEngine,
        ILogger<LoanPaymentService> logger)
    {
        _context = context;
        _allocationEngine = allocationEngine;
        _accrualEngine = accrualEngine;
        _postingEngine = postingEngine;
        _stateEngine = stateEngine;
        _logger = logger;
    }

    public async Task<Guid> ProcessPaymentAsync(
        Guid loanId,
        decimal amount,
        string reference,
        CancellationToken ct = default)
    {
        if (amount <= 0)
            throw new DomainException(Cobryx.Domain.Common.DomainErrorCode.Loans.InvalidPaymentAmount);

        // 1. Transactional Atomicity
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _context.BeginTransactionAsync(ct: ct);
            try
            {
                var loan = await _context.Loans
                    .Include(l => l.Agreement)
                        .ThenInclude(a => a.PaymentApplicationPolicy)
                    .Include(l => l.Installments)
                    .FirstOrDefaultAsync(l => l.Id == loanId, ct)
                    ?? throw new DomainException(Cobryx.Domain.Common.DomainErrorCode.Loans.CreditSaleNotFound);

                // 2. Flush Accruals (Ensure balances are current)
                // We must catch up interest and fees to today's date before allocating the payment.
                await _accrualEngine.ProcessLoanAccrualAsync(loan, DateTime.UtcNow.Date, ct);

                // 3. Allocate Payment
                var paymentId = Guid.NewGuid();
                var allocation = await _allocationEngine.AllocateAsync(loan, amount, paymentId, ct);

                // 4. Record Allocation Audit
                _context.LoanPaymentAllocations.Add(allocation);

                // 5. Post to Ledger
                await _postingEngine.PostLoanPaymentAllocationAsync(loan, allocation, reference, ct: ct);

                // 6. Update Loan Projection
                loan.ApplyAllocation(allocation);

                await _context.SaveChangesAsync(ct);
                await dbTransaction.CommitAsync(ct);

                _logger.LogInformation("Payment {Reference} processed for Loan {LoanId}. Principal: {P}, Interest: {I}, Fees: {F}",
                    reference, loanId, allocation.PrincipalApplied, allocation.InterestApplied, allocation.FeesApplied);

                return allocation.Id;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error processing payment for Loan {LoanId}", loanId);
                throw;
            }
        });
    }
}
