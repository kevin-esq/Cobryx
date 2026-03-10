using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Services;
using Cobryx.Application.Accounting.Services;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Lending.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Services.Lending;

public class CollectionsEngine : ICollectionsEngine
{
    private readonly ICobryxDbContext _context;
    private readonly ILoanAccrualEngine _accrualEngine;
    private readonly FinancialPostingEngine _postingEngine;
    private readonly ILogger<CollectionsEngine> _logger;

    public CollectionsEngine(
        ICobryxDbContext context,
        ILoanAccrualEngine accrualEngine,
        FinancialPostingEngine postingEngine,
        ILogger<CollectionsEngine> logger)
    {
        _context = context;
        _accrualEngine = accrualEngine;
        _postingEngine = postingEngine;
        _logger = logger;
    }

    public async Task RunDailyEvaluationAsync(DateTime today, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Daily Collections Evaluation for {Today}", today);

        // Batch processing active loans
        var loanIds = await _context.Loans
            .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Suspended)
            .Select(l => l.Id)
            .ToListAsync(ct);

        foreach (var loanId in loanIds)
        {
            await EvaluateLoanAsync(loanId, today, ct);
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Daily Collections Evaluation Completed.");
    }

    public async Task EvaluateLoanAsync(Guid loanId, DateTime today, CancellationToken ct = default)
    {
        var loan = await _context.Loans
            .Include(l => l.DelinquencyState)
            .Include(l => l.Installments)
            .Include(l => l.Agreement)
                .ThenInclude(a => a.LateFeePolicy)
            .FirstOrDefaultAsync(l => l.Id == loanId, ct);

        if (loan == null) return;

        var policy = await GetPolicyForTenantAsync(loan.TenantId, ct);

        // 1. Calculate DPD (Bank Grade: oldest unpaid installment)
        var oldestUnpaid = loan.Installments
            .Where(i => i.Status != InstallmentStatus.Paid)
            .OrderBy(i => i.DueDate)
            .FirstOrDefault();

        int dpd = 0;
        DateTime? oldestDueDate = null;

        if (oldestUnpaid != null && oldestUnpaid.DueDate < today)
        {
            dpd = (today - oldestUnpaid.DueDate).Days;
            oldestDueDate = oldestUnpaid.DueDate;
        }

        // 2. Determine Stage based on Policy
        var stage = MapDpdToStage(dpd, policy);

        // 3. Update Materialized State
        var state = loan.DelinquencyState;
        if (state == null)
        {
            state = new LoanDelinquencyState(loan.Id);
            _context.LoanDelinquencyStates.Add(state);
        }

        // Idempotency check: Don't evaluate twice on the same day
        if (state.LastEvaluatedDate?.Date == today.Date)
        {
            _logger.LogInformation("Loan {LoanId} already evaluated for {Today}. Skipping.", loan.Id, today.Date);
            return;
        }

        var oldStage = state.Stage;
        state.UpdateState(dpd, oldestDueDate, stage, DateTime.UtcNow, today.Date);

        // 4. Handle Escalations / Events
        if (stage != oldStage)
        {
            await HandleStageTransitionAsync(loan, oldStage, stage, dpd, ct);
        }

        // 5. Automated Late Fees
        // Handled by LoanAccrualEngine, honors CollectionsPolicy.EnableLateFees.

        // 6. Automated Write-Off (if applicable)
        if (policy.EnableAutoWriteOff && dpd >= policy.WriteOffDays && !loan.IsWrittenOff)
        {
            await _postingEngine.PostChargeOffAsync(loan, $"Automated Write-Off at {dpd} DPD", ct);
            loan.MarkAsWrittenOff(today);
            _logger.LogWarning("Loan {LoanId} reached Write-Off threshold ({Dpd} DPD). Automated Charge-Off posted.", loan.Id, dpd);
        }
    }

    private async Task<CollectionsPolicy> GetPolicyForTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var policy = await _context.CollectionsPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, ct);

        return policy ?? CollectionsPolicy.CreateStandard(tenantId, "Default Collections Policy");
    }

    private DelinquencyStage MapDpdToStage(int dpd, CollectionsPolicy policy)
    {
        if (dpd <= 0) return DelinquencyStage.Current;
        if (dpd < policy.ModerateStageDays) return DelinquencyStage.Early;
        if (dpd < policy.SevereStageDays) return DelinquencyStage.Moderate;
        if (dpd < policy.DefaultStageDays) return DelinquencyStage.Severe;
        if (dpd < policy.WriteOffDays) return DelinquencyStage.Default;
        return DelinquencyStage.WriteOff;
    }

    private async Task HandleStageTransitionAsync(Loan loan, DelinquencyStage oldStage, DelinquencyStage newStage, int dpd, CancellationToken ct)
    {
        var type = newStage > oldStage ? CollectionsEventType.StageEscalated : CollectionsEventType.StageDeescalated;
        var description = $"Transitioned from {oldStage} to {newStage} at {dpd} DPD.";

        var collectionsEvent = new LoanCollectionsEvent(
            loan.TenantId,
            loan.Id,
            type,
            dpd,
            description);

        _context.LoanCollectionsEvents.Add(collectionsEvent);
        _logger.LogInformation("Loan {LoanId} Stage Change: {Old} -> {New}", loan.Id, oldStage, newStage);

        await Task.CompletedTask;
    }
}
