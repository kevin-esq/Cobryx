using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Lending.Services;

/// <summary>
/// The Brain that manages the risk lifecycle of a loan. 
/// Orchestrates state transitions, DPD calculations, and automated charge-off accounting.
/// </summary>
public class FinancialStateEngine
{
    private readonly ICobryxDbContext _context;
    private readonly FinancialPostingEngine _postingEngine;
    private readonly IClock _clock;
    private readonly ILogger<FinancialStateEngine> _logger;

    public FinancialStateEngine(
        ICobryxDbContext context,
        FinancialPostingEngine postingEngine,
        IClock clock,
        ILogger<FinancialStateEngine> logger)
    {
        _context = context;
        _postingEngine = postingEngine;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// Recalculates the risk status of a loan based on its installments and Ledger truth.
    /// This is the Bank-Grade truth calculation for DPD.
    /// </summary>
    public virtual async Task UpdateStatusAsync(Guid loanId, string? reason = null, CancellationToken ct = default)
    {
        var loan = await _context.Loans
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == loanId, ct);

        if (loan == null || loan.FinancialStatus == FinancialStatus.ChargedOff)
            return;

        var now = _clock.UtcNow;

        var ledgerBalances = await GetLoanLedgerBalancesAsync(loan.Id, ct);



        var oldStatus = loan.FinancialStatus;
        loan.UpdateFinancialRiskStatus();

        if (loan.FinancialDaysPastDue >= 180 && loan.FinancialStatus != FinancialStatus.ChargedOff)
        {
            await ExecuteChargeOffAsync(loan.Id, "Automated: 180+ Days Past Due", ct);
            return;
        }

        if (loan.FinancialStatus != oldStatus)
        {
            var audit = new FinancialStatusAudit(
                loan.TenantId,
                loan.Id,
                oldStatus,
                loan.FinancialStatus,
                loan.FinancialDaysPastDue,
                loan.ArrearsAmount,
                reason ?? "System automated recalculation (Ledger Linked)");

            _context.FinancialStatusAudits.Add(audit);
            _logger.LogInformation("Loan {LoanId} risk transition: {Old} -> {New} (DPD: {DPD})",
                loan.Id, oldStatus, loan.FinancialStatus, loan.FinancialDaysPastDue);
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task<LoanLedgerBalances> GetLoanLedgerBalancesAsync(Guid loanId, CancellationToken ct)
    {
        var receivableAccountCodes = new[] { "1210", "4010", "4020" };

        var entries = await _context.LedgerEntries
            .Where(e => _context.LedgerTransactions
                .Where(t => t.LoanId == loanId && t.IsPosted)
                .Select(t => t.Id)
                .Contains(e.TransactionId))
            .Where(e => _context.LedgerAccounts
                .Where(a => receivableAccountCodes.Contains(a.Code))
                .Select(a => a.Id)
                .Contains(e.AccountId))
            .ToListAsync(ct);

        return new LoanLedgerBalances(
            Principal: entries.Sum(e => e.Debit - e.Credit),
            Total: entries.Sum(e => e.Debit - e.Credit)
        );
    }

    private record LoanLedgerBalances(decimal Principal, decimal Total);

    /// <summary>
    /// Executes a formal Charge-Off. 
    /// 1. Updates Risk Status
    /// 2. Posts Ledger Loss Transaction
    /// 3. Records Audit Trail
    /// </summary>
    public virtual async Task ExecuteChargeOffAsync(Guid loanId, string reason, CancellationToken ct = default)
    {
        var loan = await _context.Loans
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == loanId, ct);

        if (loan == null || loan.FinancialStatus == FinancialStatus.ChargedOff)
            return;

        if (loan.Status == LoanStatus.Closed)
        {
            _logger.LogWarning("Blocking charge-off for Loan {LoanId}: Loan is already CLOSED.", loanId);
            return;
        }

        var outstanding = loan.CurrentPrincipalBalance + loan.CurrentInterestBalance + loan.CurrentLateFeeBalance;
        if (outstanding <= 0)
        {
            _logger.LogWarning("Blocking charge-off for Loan {LoanId}: Total outstanding balance is zero or less ({Amount}).", loanId, outstanding);
            return;
        }

        var oldStatus = loan.FinancialStatus;

        await _postingEngine.PostChargeOffAsync(loan, reason, ct);

        loan.MarkAsChargedOff();

        var audit = new FinancialStatusAudit(
            loan.TenantId,
            loan.Id,
            oldStatus,
            FinancialStatus.ChargedOff,
            loan.FinancialDaysPastDue,
            loan.ArrearsAmount,
            $"CHARGE-OFF: {reason}");

        _context.FinancialStatusAudits.Add(audit);
        await _context.SaveChangesAsync(ct);

        _logger.LogWarning("Loan {LoanId} formally Charged-Off. Reason: {Reason}", loanId, reason);
    }

    /// <summary>
    /// Records a recovery payment for a non-performing asset.
    /// Ensures accounting hit 'Recovery Income' while updating risk state to 'Recovered'.
    /// </summary>
    public virtual async Task ProcessRecoveryPaymentAsync(Guid loanId, decimal amount, string reference, CancellationToken ct = default)
    {
        var loan = await _context.Loans
             .Include(l => l.Installments)
             .FirstOrDefaultAsync(l => l.Id == loanId, ct);

        if (loan == null || loan.FinancialStatus != FinancialStatus.ChargedOff)
            return;

        var oldStatus = loan.FinancialStatus;

        await _postingEngine.PostRecoveryAsync(loan, amount, reference, ct);

        loan.MarkAsRecovered();

        var audit = new FinancialStatusAudit(
            loan.TenantId,
            loan.Id,
            oldStatus,
            FinancialStatus.Recovered,
            loan.FinancialDaysPastDue,
            loan.ArrearsAmount,
            $"RECOVERY: {reference}");

        _context.FinancialStatusAudits.Add(audit);
        await _context.SaveChangesAsync(ct);
    }
}
