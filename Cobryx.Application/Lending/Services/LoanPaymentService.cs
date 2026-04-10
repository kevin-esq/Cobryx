using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Lending.Services;

public partial class LoanPaymentService(
    ICobryxDbContext context,
    IPaymentAllocationEngine allocationEngine,
    ILoanAccrualEngine accrualEngine,
    FinancialPostingEngine postingEngine,
    IClock clock,
    ILogger<LoanPaymentService> logger)
{
    public async Task<Guid> ProcessPaymentAsync(
        Guid loanId,
        decimal amount,
        string reference,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            throw new DomainException(DomainErrorCode.Loans.InvalidPaymentAmount);
        }

        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using IDbContextTransaction dbTransaction = await context.BeginTransactionAsync(ct: ct);
            try
            {
                Loan loan = await context.Loans
                    .Include(static l => l.Agreement)
                        .ThenInclude(static a => a.PaymentApplicationPolicy)
                    .Include(static l => l.Installments)
                    .FirstOrDefaultAsync(l => l.Id == loanId, ct)
                    ?? throw new DomainException(DomainErrorCode.Loans.CreditSaleNotFound);

                await accrualEngine.ProcessLoanAccrualAsync(loan, clock.UtcNow.Date, ct);

                Guid paymentId = Guid.NewGuid();
                LoanPaymentAllocation allocation = await allocationEngine.AllocateAsync(loan, amount, paymentId, ct);

                context.LoanPaymentAllocations.Add(allocation);

                await postingEngine.PostLoanPaymentAllocationAsync(loan, allocation, reference, ct: ct);

                loan.ApplyAllocation(allocation);

                await context.SaveChangesAsync(ct);
                await dbTransaction.CommitAsync(ct);

                LogPaymentProcessed(logger, reference, loanId, allocation.PrincipalApplied, allocation.InterestApplied, allocation.FeesApplied);

                return allocation.Id;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync(ct);
                LogPaymentError(logger, ex, loanId);
                throw;
            }
        });
    }
}
