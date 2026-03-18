using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Lending;

public class LoanAccrualEngine : ILoanAccrualEngine
{
    private readonly ICobryxDbContext _dbContext;
    private readonly ILateFeeService _lateFeeService;
    private readonly ILogger<LoanAccrualEngine> _logger;
    private const int BatchSize = 1000;

    /// <summary>
    /// Financial engine responsible for daily interest calculation and late fee assessment.
    /// </summary>
    /// <remarks>
    /// This engine implements a "Waterfall" catch-up logic, ensuring that if the batch
    /// fails for one day, the next run will recover all missing accrual days sequentially
    /// to maintain ledger integrity.
    /// </remarks>
    public LoanAccrualEngine(
        ICobryxDbContext dbContext,
        ILateFeeService lateFeeService,
        ILogger<LoanAccrualEngine> logger)
    {
        _dbContext = dbContext;
        _lateFeeService = lateFeeService;
        _logger = logger;
    }

    public async Task RunDailyAccrualAsync(DateTime accrualDate, CancellationToken ct = default)
    {
        var targetDate = accrualDate.Date;
        _logger.LogInformation("Starting daily accrual for {AccrualDate}", targetDate);

        int totalProcessed = 0;
        int totalChargesCreated = 0;

        while (true)
        {
            // Implementation of 'Sequential Catch-up': we fetch loans whose LastAccrualDate
            // is behind the targetDate. This allows for safe recovery after system downtime.
            var loans = await _dbContext.Loans
                .Include(l => l.Agreement)
                .ThenInclude(a => a.InterestPolicy)
                .Where(l => l.Status == LoanStatus.Active && l.LastAccrualDate < targetDate)
                .OrderBy(l => l.Id) // Sorted by Id for predictable batching
                .Take(BatchSize)
                .ToListAsync(ct);

            if (loans.Count == 0)
                break;

            foreach (var loan in loans)
            {
                try
                {
                    totalChargesCreated += await ProcessLoanAccrualAsync(loan, targetDate, ct);
                    totalProcessed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process accrual for Loan {LoanId}", loan.Id);
                }
            }

            // Explicitly await SaveChangesAsync
            await _dbContext.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Finished daily accrual. Processed {LoanCount} loans, created {ChargeCount} charges.", totalProcessed, totalChargesCreated);
    }

    public async Task<int> ProcessLoanAccrualAsync(Cobryx.Domain.Lending.Loan loan, DateTime targetDate, CancellationToken ct = default)
    {
        int chargesCreated = 0;
        var nextDate = loan.LastAccrualDate.AddDays(1).Date;
        var policy = await GetCollectionsPolicyAsync(loan.TenantId, ct);

        // Waterfall recovery: we iterate through every missing day until targetDate is reached.
        while (nextDate <= targetDate)
        {
            // 1. Calculate Ordinary Interest: derived from current OutstandingPrincipal.
            var dailyInterest = CalculateDailyInterest(loan);
            if (dailyInterest > 0)
            {
                var charge = new AccruedCharge(loan.Id, ChargeType.OrdinaryInterest, dailyInterest, nextDate);
                loan.AddAccruedCharge(charge);
                chargesCreated++;
            }

            // 2. Assess Late Fees: Policy-driven assessment based on DPD (Days Past Due).
            if (policy.EnableLateFees)
            {
                // We increment chargesCreated based on whether a new fee was actually generated.
                chargesCreated += (int)Math.Min(1, _lateFeeService.AssessLateFee(loan, nextDate));
            }

            loan.MarkAccrued(nextDate);
            nextDate = nextDate.AddDays(1);
        }

        return chargesCreated;
    }

    private async Task<CollectionsPolicy> GetCollectionsPolicyAsync(Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.CollectionsPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, ct)
            ?? CollectionsPolicy.CreateStandard(tenantId, "Default Collections Policy");
    }

    private static decimal CalculateDailyInterest(Loan loan)
    {
        var policy = loan.Agreement?.InterestPolicy;
        if (policy == null || !policy.IsActive)
            return 0;

        var dailyRate = policy.CalculateDailyRate();

        // High-precision rounding is deferred to the FinancialPostingEngine to avoid
        // compounding errors during daily aggregation.
        return loan.OutstandingPrincipal * dailyRate;
    }
}
