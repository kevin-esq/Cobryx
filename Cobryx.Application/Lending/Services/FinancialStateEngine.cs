using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Lending.Services;

/// <summary>
/// The Brain that manages the risk lifecycle of a loan.
/// </summary>
public partial class FinancialStateEngine(
    ICobryxDbContext context,
    FinancialPostingEngine postingEngine,
    ILogger<FinancialStateEngine> logger)
{
    /// <summary>
    /// Recalculates the risk status of a loan based on its installments and Ledger truth.
    /// This is the Bank-Grade truth calculation for DPD.
    /// </summary>
    public virtual async Task UpdateStatusAsync(Guid loanId, string? reason = null, CancellationToken ct = default)
    {
        Loan? loan = await context.Loans
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == loanId, ct);

        if (loan == null || loan.FinancialStatus == FinancialStatus.ChargedOff)
        {
            return;
        }

        FinancialStatus oldStatus = loan.FinancialStatus;
        loan.UpdateFinancialRiskStatus();

        if (loan.FinancialDaysPastDue >= 180 && loan.FinancialStatus != FinancialStatus.ChargedOff)
        {
            await ExecuteChargeOffAsync(loan.Id, "Automated: 180+ Days Past Due", ct);
            return;
        }

        if (loan.FinancialStatus != oldStatus)
        {
            FinancialStatusAudit audit = new FinancialStatusAudit(
                loan.TenantId,
                loan.Id,
                oldStatus,
                loan.FinancialStatus,
                loan.FinancialDaysPastDue,
                loan.ArrearsAmount,
                reason ?? "System automated recalculation (Ledger Linked)");

            context.FinancialStatusAudits.Add(audit);
            LogRiskTransition(logger, loan.Id, oldStatus, loan.FinancialStatus, loan.FinancialDaysPastDue);
        }

        await context.SaveChangesAsync(ct);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Loan {LoanId} risk transition: {Old} -> {New} (DPD: {DPD})")]
    static partial void LogRiskTransition(ILogger logger, Guid loanId, FinancialStatus old, FinancialStatus @new,
        int dpd);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Blocking charge-off for Loan {LoanId}: Loan is already CLOSED.")]
    static partial void LogChargeOffBlockedClosed(ILogger logger, Guid loanId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning,
        Message = "Blocking charge-off for Loan {LoanId}: Total outstanding balance is zero or less ({Amount}).")]
    static partial void LogChargeOffBlockedZeroBalance(ILogger logger, Guid loanId, decimal amount);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning,
        Message = "Loan {LoanId} formally Charged-Off. Reason: {Reason}")]
    static partial void LogLoanChargedOff(ILogger logger, Guid loanId, string reason);

    /// <summary>
    /// Executes a formal Charge-Off.
    /// 1. Updates Risk Status
    /// 2. Posts Ledger Loss Transaction
    /// 3. Records Audit Trail
    /// </summary>
    public virtual async Task ExecuteChargeOffAsync(Guid loanId, string reason, CancellationToken ct = default)
    {
        Loan? loan = await context.Loans
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == loanId, ct);

        if (loan == null || loan.FinancialStatus == FinancialStatus.ChargedOff)
        {
            return;
        }

        if (loan.Status == LoanStatus.Closed)
        {
            LogChargeOffBlockedClosed(logger, loanId);
            return;
        }

        decimal outstanding = loan.CurrentPrincipalBalance + loan.CurrentInterestBalance + loan.CurrentLateFeeBalance;
        if (outstanding <= 0)
        {
            LogChargeOffBlockedZeroBalance(logger, loanId, outstanding);
            return;
        }

        FinancialStatus oldStatus = loan.FinancialStatus;

        await postingEngine.PostChargeOffAsync(loan, reason, ct);

        loan.MarkAsChargedOff();

        FinancialStatusAudit audit = new FinancialStatusAudit(
            loan.TenantId,
            loan.Id,
            oldStatus,
            FinancialStatus.ChargedOff,
            loan.FinancialDaysPastDue,
            loan.ArrearsAmount,
            $"CHARGE-OFF: {reason}");

        context.FinancialStatusAudits.Add(audit);
        await context.SaveChangesAsync(ct);

        LogLoanChargedOff(logger, loanId, reason);
    }
}
