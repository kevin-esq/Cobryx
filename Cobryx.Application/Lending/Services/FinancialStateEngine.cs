using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Lending.Enums;
using Cobryx.Application.Accounting.Services;
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
    /// Recalculates the risk status of a loan based on its installments.
    /// This is the Bank-Grade truth calculation for DPD.
    /// </summary>
    public virtual async Task UpdateStatusAsync(Guid loanId, string? reason = null, CancellationToken ct = default)
    {
        var loan = await _context.Loans
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == loanId, ct);

        if (loan == null) return;

        var oldStatus = loan.FinancialStatus;
        var now = _clock.UtcNow;

        // 1. Logic is encapsulated in the domain entity
        loan.UpdateFinancialRiskStatus(now);

        // 2. Audit the transition if status changed
        if (loan.FinancialStatus != oldStatus)
        {
            var audit = new FinancialStatusAudit(
                loan.TenantId,
                loan.Id,
                oldStatus,
                loan.FinancialStatus,
                loan.FinancialDaysPastDue,
                loan.ArrearsAmount,
                reason ?? "System automated recalculation");

            _context.FinancialStatusAudits.Add(audit);
            _logger.LogInformation("Loan {LoanId} risk transition: {Old} -> {New} (DPD: {DPD})",
                loan.Id, oldStatus, loan.FinancialStatus, loan.FinancialDaysPastDue);
        }

        await _context.SaveChangesAsync(ct);
    }

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

        if (loan == null || loan.FinancialStatus == FinancialStatus.ChargedOff) return;

        var oldStatus = loan.FinancialStatus;

        // 1. Transactional Accounting
        await _postingEngine.PostChargeOffAsync(loan, reason, ct);

        // 2. Domain Transition
        loan.MarkAsChargedOff();

        // 3. Audit Trail
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

        if (loan == null || loan.FinancialStatus != FinancialStatus.ChargedOff) return;

        var oldStatus = loan.FinancialStatus;

        // 1. Recovery Accounting
        await _postingEngine.PostRecoveryAsync(loan, amount, reference, ct);

        // 2. Domain Transition
        loan.MarkAsRecovered();

        // 3. Audit Trail
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
