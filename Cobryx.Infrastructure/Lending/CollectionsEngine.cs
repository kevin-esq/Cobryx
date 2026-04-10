using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Lending;

/// <summary>
/// Engine specialized in monitoring loan delinquency and executing policy-driven escalations.
/// </summary>
/// <remarks>
/// This service determines the "Days Past Due" (DPD) using the oldest unpaid installment
/// as the reference (Bank Grade). It orchestrates stage transitions and automated write-offs.
/// </remarks>
public partial class CollectionsEngine(
    ICobryxDbContext context,
    FinancialPostingEngine postingEngine,
    ILogger<CollectionsEngine> logger) : ICollectionsEngine
{
    public async Task RunDailyEvaluationAsync(DateTime today, CancellationToken ct = default)
    {
        LogStartingDailyEvaluation(logger, today);

        List<Guid> loanIds = await context.Loans
            .Where(static l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Suspended)
            .Select(static l => l.Id)
            .ToListAsync(ct);

        foreach (Guid loanId in loanIds)
        {
            await EvaluateLoanAsync(loanId, today, ct);
        }

        await context.SaveChangesAsync(ct);
        LogDailyEvaluationCompleted(logger);
    }

    public async Task EvaluateLoanAsync(Guid loanId, DateTime today, CancellationToken ct = default)
    {
        Loan? loan = await context.Loans
            .Include(static l => l.DelinquencyState)
            .Include(static l => l.Installments)
            .Include(static l => l.Agreement)
                .ThenInclude(static a => a.LateFeePolicy)
            .FirstOrDefaultAsync(l => l.Id == loanId, ct);

        if (loan == null)
        {
            return;
        }

        CollectionsPolicy policy = await GetPolicyForTenantAsync(loan.TenantId, ct);

        Installment? oldestUnpaid = loan.Installments
            .Where(static i => i.Status != InstallmentStatus.Paid)
            .OrderBy(static i => i.DueDate)
            .FirstOrDefault();

        int dpd = 0;
        DateTime? oldestDueDate = null;

        if (oldestUnpaid != null && oldestUnpaid.DueDate < today)
        {
            dpd = (today - oldestUnpaid.DueDate).Days;
            oldestDueDate = oldestUnpaid.DueDate;
        }

        DelinquencyStage stage = MapDpdToStage(dpd, policy);

        LoanDelinquencyState? state = loan.DelinquencyState;
        if (state == null)
        {
            state = new LoanDelinquencyState(loan.Id);
            context.LoanDelinquencyStates.Add(state);
        }

        if (state.LastEvaluatedDate?.Date == today.Date)
        {
            LogLoanAlreadyEvaluated(logger, loan.Id, today.Date);
            return;
        }

        DelinquencyStage oldStage = state.Stage;
        state.UpdateState(dpd, oldestDueDate, stage, DateTime.UtcNow, today.Date);

        if (stage != oldStage)
        {
            await HandleStageTransitionAsync(loan, oldStage, stage, dpd, ct);
        }

        if (policy.EnableAutoWriteOff && dpd >= policy.WriteOffDays && !loan.IsWrittenOff)
        {
            await postingEngine.PostChargeOffAsync(loan, $"Automated Write-Off at {dpd} DPD", ct);
            loan.MarkAsWrittenOff(today);
            LogAutoWriteOffPosted(logger, loan.Id, dpd);
        }
    }

    private async Task<CollectionsPolicy> GetPolicyForTenantAsync(Guid tenantId, CancellationToken ct)
    {
        CollectionsPolicy? policy = await context.CollectionsPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, ct);

        return policy ?? CollectionsPolicy.CreateStandard(tenantId, "Default Collections Policy");
    }

    private static DelinquencyStage MapDpdToStage(int dpd, CollectionsPolicy policy)
    {
        if (dpd <= 0)
            return DelinquencyStage.Current;
        if (dpd < policy.ModerateStageDays)
            return DelinquencyStage.Early;
        if (dpd < policy.SevereStageDays)
            return DelinquencyStage.Moderate;
        if (dpd < policy.DefaultStageDays)
            return DelinquencyStage.Severe;
        if (dpd < policy.WriteOffDays)
            return DelinquencyStage.Default;
        return DelinquencyStage.WriteOff;
    }

    private async Task HandleStageTransitionAsync(Loan loan, DelinquencyStage oldStage, DelinquencyStage newStage, int dpd, CancellationToken ct)
    {
        _ = ct;
        CollectionsEventType type = newStage > oldStage ? CollectionsEventType.StageEscalated : CollectionsEventType.StageDeescalated;
        string description = $"Transitioned from {oldStage} to {newStage} at {dpd} DPD.";

        LoanCollectionsEvent collectionsEvent = new LoanCollectionsEvent(
            loan.TenantId,
            loan.Id,
            type,
            dpd,
            description);

        context.LoanCollectionsEvents.Add(collectionsEvent);
        LogLoanStageChanged(logger, loan.Id, oldStage, newStage);

        await Task.CompletedTask;
    }
}
