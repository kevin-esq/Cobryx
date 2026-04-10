using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Lending
{
    /// <summary>
    /// Financial engine responsible for daily interest calculation and late fee assessment.
    /// </summary>
    /// <remarks>
    /// This engine implements a "Waterfall" catch-up logic, ensuring that if the batch
    /// fails for one day, the next run will recover all missing accrual days sequentially
    /// to maintain ledger integrity.
    /// </remarks>
    public partial class LoanAccrualEngine(
        ICobryxDbContext dbContext,
        ILateFeeService lateFeeService,
        ILogger<LoanAccrualEngine> logger) : ILoanAccrualEngine
    {
        private const int BatchSize = 1000;

        public async Task RunDailyAccrualAsync(DateTime accrualDate, CancellationToken ct = default)
        {
            DateTime targetDate = accrualDate.Date;
            LogStartingDailyAccrual(logger, targetDate);

            var totalProcessed = 0;
            var totalChargesCreated = 0;

            while (true)
            {
                List<Loan> loans = await dbContext.Loans
                    .Include(static l => l.Agreement)
                    .ThenInclude(static a => a.InterestPolicy)
                    .Where(l => l.Status == LoanStatus.Active && l.LastAccrualDate < targetDate)
                    .OrderBy(static l => l.Id)
                    .Take(BatchSize)
                    .ToListAsync(ct);

                if (loans.Count == 0)
                {
                    break;
                }

                foreach (Loan loan in loans)
                {
                    try
                    {
                        totalChargesCreated += await ProcessLoanAccrualAsync(loan, targetDate, ct);
                        totalProcessed++;
                    }
                    catch (Exception ex)
                    {
                        LogAccrualProcessingFailed(logger, ex, loan.Id);
                    }
                }

                _ = await dbContext.SaveChangesAsync(ct);
            }

            LogDailyAccrualFinished(logger, totalProcessed, totalChargesCreated);
        }

        public async Task<int> ProcessLoanAccrualAsync(Loan loan, DateTime targetDate, CancellationToken ct = default)
        {
            var chargesCreated = 0;
            DateTime nextDate = loan.LastAccrualDate.AddDays(1).Date;
            CollectionsPolicy policy = await GetCollectionsPolicyAsync(loan.TenantId, ct);

            while (nextDate <= targetDate)
            {
                var dailyInterest = CalculateDailyInterest(loan);
                if (dailyInterest > 0)
                {
                    AccruedCharge charge = new(loan.Id, ChargeType.OrdinaryInterest, dailyInterest, nextDate);
                    loan.AddAccruedCharge(charge);
                    chargesCreated++;
                }

                if (policy.EnableLateFees)
                {
                    chargesCreated += (int)Math.Min(1, lateFeeService.AssessLateFee(loan, nextDate));
                }

                loan.MarkAccrued(nextDate);
                nextDate = nextDate.AddDays(1);
            }

            return chargesCreated;
        }

        private async Task<CollectionsPolicy> GetCollectionsPolicyAsync(Guid tenantId, CancellationToken ct)
        {
            CollectionsPolicy? policy = await dbContext.CollectionsPolicies
                .FirstOrDefaultAsync(p => p.TenantId == tenantId, ct);

            return policy ?? CollectionsPolicy.CreateStandard(tenantId, "Default Collections Policy");
        }

        private static decimal CalculateDailyInterest(Loan loan)
        {
            InterestPolicy policy = loan.Agreement.InterestPolicy;
            if (!policy.IsActive)
            {
                return 0;
            }

            var dailyRate = policy.CalculateDailyRate();

            return loan.OutstandingPrincipal * dailyRate;
        }
    }
}
