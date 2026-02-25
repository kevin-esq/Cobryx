using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Accounting;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services;

/// <summary>
/// The "Brain" of the financial system. Orchestrates how payments are recorded in the Ledger.
/// Enforces deterministic splits and atomic journal creation.
/// </summary>
public class FinancialPostingEngine
{
    private readonly ICobryxDbContext _context;
    private readonly ILogger<FinancialPostingEngine> _logger;

    public FinancialPostingEngine(ICobryxDbContext context, ILogger<FinancialPostingEngine> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Processes a payment against a loan and creates the corresponding Ledger Transaction.
    /// Handles deterministic split between Principal, Interest and Fees.
    /// </summary>
    public async Task<Guid> PostLoanPaymentAsync(
        Loan loan,
        decimal amount,
        string reference,
        decimal? platformFee = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Posting payment of {Amount} for Loan {LoanId}", amount, loan.Id);

        // 1. Get System Accounts for the Tenant
        var accounts = await GetTenantSystemAccountsAsync(loan.TenantId, ct);

        // 2. Determine Allocation (Deterministic Split)
        // Note: For MVP, we use the simple Interest -> Principal split logic provided by the expert.
        var split = CalculatePaymentSplit(loan, amount);

        // 3. Create Atomic Journal (LedgerTransaction)
        var transaction = new LedgerTransaction(
            loan.TenantId,
            $"Loan Payment - {reference}",
            $"PAY-{reference}",
            loan.Id);

        // Debit CASH (Asset Increases)
        transaction.AddEntry(accounts.CashAccountId, amount, 0);

        // Credit A/R Principal (Asset Decreases)
        if (split.PrincipalAmount > 0)
        {
            transaction.AddEntry(accounts.PrincipalAccountId, 0, split.PrincipalAmount);
        }

        // Credit Interest Revenue (Revenue Increases)
        if (split.InterestAmount > 0)
        {
            transaction.AddEntry(accounts.InterestAccountId, 0, split.InterestAmount);
        }

        // Credit Fee Revenue (Revenue Increases)
        if (split.FeeAmount > 0)
        {
            transaction.AddEntry(accounts.FeeAccountId, 0, split.FeeAmount);
        }

        // 4. Finalize & Post
        transaction.Post();

        _context.LedgerTransactions.Add(transaction);

        // 5. Dual-Ledger: Record Platform Fee Revenue (if applicable)
        if (platformFee.HasValue && platformFee.Value > 0)
        {
            var platformAccounts = await GetTenantSystemAccountsAsync(CobryxDefaults.PlatformTenantId, ct);

            var platformTx = new LedgerTransaction(
                CobryxDefaults.PlatformTenantId,
                $"Platform Fee - {reference} (Tenant: {loan.TenantId})",
                $"FEE-{reference}");

            // Debit Platform Receivable (Asset Increases)
            platformTx.AddEntry(platformAccounts.CashAccountId, platformFee.Value, 0);

            // Credit Fee Revenue (Revenue Increases)
            platformTx.AddEntry(platformAccounts.FeeAccountId, 0, platformFee.Value);

            platformTx.Post();
            _context.LedgerTransactions.Add(platformTx);
        }

        _logger.LogInformation(
            "Financial Posting Finished: Principal: {P}, Interest: {I}, Fees: {F}, Platform Fee: {PF}",
            split.PrincipalAmount, split.InterestAmount, split.FeeAmount, platformFee ?? 0);

        return transaction.Id;
    }

    /// <summary>
    /// Creates a Mirror Reversal for an existing transaction.
    /// Used for Refunds, Chargebacks, and Storno entries.
    /// </summary>
    /// <summary>
    /// Creates a Mirror Reversal for an existing transaction.
    /// Used for Refunds, Chargebacks, and Storno entries.
    /// </summary>
    public async Task<Guid> PostReversalAsync(Guid originalTransactionId, decimal amount, string reason, CancellationToken ct = default)
    {
        var original = await _context.LedgerTransactions
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == originalTransactionId, ct);

        if (original == null)
            throw new DomainException(DomainErrorCode.Common.GeneralError);

        // 1. Drift Protection: How much has been reversed already?
        var existingReversals = await _context.LedgerTransactions
            .Where(t => t.OriginalTransactionId == originalTransactionId && t.IsPosted)
            .Include(t => t.Entries)
            .ToListAsync(ct);

        var alreadyReversed = existingReversals.Sum(t => t.Entries.Sum(e => Math.Abs(e.Debit)));
        var originalTotal = original.Entries.Sum(e => Math.Abs(e.Debit));
        var remainingReversibleAmount = Math.Max(0, originalTotal - alreadyReversed);

        if (amount > remainingReversibleAmount + 0.01m)
        {
            _logger.LogWarning("Over-reversal blocked for Transaction {Id}. Requested: {Requested}, Remaining: {Remaining}",
                originalTransactionId, amount, remainingReversibleAmount);
            throw new DomainException(DomainErrorCode.Common.GeneralError); // "Refund exceeds remaining balance"
        }

        var reversal = LedgerTransaction.CreatePartialReversal(original, amount, $"REVERSAL: {reason}");
        _context.LedgerTransactions.Add(reversal);

        // 2. Dual-Ledger: Reverse Platform Fee if applicable
        if (original.ReferenceId?.StartsWith("PAY-") == true)
        {
            var feeRef = original.ReferenceId.Replace("PAY-", "FEE-");
            var platformTx = await _context.LedgerTransactions
                .Include(t => t.Entries)
                .FirstOrDefaultAsync(t => t.ReferenceId == feeRef && t.TenantId == CobryxDefaults.PlatformTenantId, ct);

            if (platformTx != null)
            {
                // Reverse the platform fee proportionally
                var platformTotal = platformTx.Entries.Sum(e => Math.Abs(e.Debit));
                var ratio = amount / originalTotal;
                var platformRefundAmount = Math.Round(platformTotal * ratio, 2);

                var platformReversal = LedgerTransaction.CreatePartialReversal(platformTx, platformRefundAmount, $"REVERSAL (FEE): {reason}");
                _context.LedgerTransactions.Add(platformReversal);

                _logger.LogInformation("Posted Proportional Platform Fee Reversal: {Amount}", platformRefundAmount);
            }
        }

        _logger.LogInformation("Posted Proportional Reversal for Transaction {OriginalId}. Amount: {Amount}", originalTransactionId, amount);

        return reversal.Id;
    }

    /// <summary>
    /// Records a Charge-Off in the Ledger.
    /// Moves the balance from Accounts Receivable (Asset) to Loss Expense (Expense).
    /// </summary>
    public async Task<Guid> PostChargeOffAsync(Loan loan, string reason, CancellationToken ct = default)
    {
        var accounts = await GetTenantSystemAccountsAsync(loan.TenantId, ct);
        var totalOutstanding = loan.CurrentPrincipalBalance + loan.CurrentInterestBalance + loan.CurrentLateFeeBalance;

        if (totalOutstanding <= 0) return Guid.Empty;

        var transaction = new LedgerTransaction(
            loan.TenantId,
            $"CHARGE-OFF ({loan.LoanNumber}): {reason}",
            $"CHG-{loan.Id}",
            loan.Id);

        // Debit LOSS EXPENSE (Expense Increases)
        transaction.AddEntry(accounts.LossExpenseId, totalOutstanding, 0);

        // Credit A/R (Assets Decrease)
        transaction.AddEntry(accounts.PrincipalAccountId, 0, loan.CurrentPrincipalBalance);
        if (loan.CurrentInterestBalance > 0)
            transaction.AddEntry(accounts.InterestAccountId, 0, loan.CurrentInterestBalance);
        if (loan.CurrentLateFeeBalance > 0)
            transaction.AddEntry(accounts.FeeAccountId, 0, loan.CurrentLateFeeBalance);

        transaction.Post();
        _context.LedgerTransactions.Add(transaction);

        _logger.LogWarning("Financial Charge-Off Posted for Loan {LoanId}. Amount: {Amount}", loan.Id, totalOutstanding);
        return transaction.Id;
    }

    /// <summary>
    /// Records a payment received AFTER a loan has been charged off.
    /// Recorded as Recovery Income (Revenue) instead of reducing A/R.
    /// </summary>
    public async Task<Guid> PostRecoveryAsync(Loan loan, decimal amount, string reference, CancellationToken ct = default)
    {
        var accounts = await GetTenantSystemAccountsAsync(loan.TenantId, ct);

        var transaction = new LedgerTransaction(
            loan.TenantId,
            $"RECOVERY: {reference}",
            $"REC-{reference}",
            loan.Id);

        // Debit CASH (Asset Increases)
        transaction.AddEntry(accounts.CashAccountId, amount, 0);

        // Credit RECOVERY INCOME (Revenue Increases)
        transaction.AddEntry(accounts.RecoveryIncomeId, 0, amount);

        transaction.Post();
        _context.LedgerTransactions.Add(transaction);

        _logger.LogInformation("Recovery Payment Posted for Loan {LoanId}. Amount: {Amount}", loan.Id, amount);
        return transaction.Id;
    }

    private async Task<TenantAccounts> GetTenantSystemAccountsAsync(Guid tenantId, CancellationToken ct)
    {
        var accounts = await _context.LedgerAccounts
            .Where(a => a.TenantId == tenantId && a.IsSystem)
            .ToListAsync(ct);

        // Helper to find by conventional codes or names
        return new TenantAccounts(
            CashAccountId: accounts.First(a => a.Code == "1010").Id,
            PrincipalAccountId: accounts.First(a => a.Code == "1210").Id,
            InterestAccountId: accounts.First(a => a.Code == "4010").Id,
            FeeAccountId: accounts.First(a => a.Code == "4020").Id,
            LossExpenseId: accounts.First(a => a.Code == "5010").Id,
            RecoveryIncomeId: accounts.First(a => a.Code == "4030").Id
        );
    }

    private PaymentSplit CalculatePaymentSplit(Loan loan, decimal amount)
    {
        var remaining = amount;

        // 1. Pay Fees first
        var feePayment = Math.Min(remaining, loan.CurrentLateFeeBalance);
        remaining -= feePayment;

        // 2. Pay Interest next
        var interestPayment = Math.Min(remaining, loan.CurrentInterestBalance);
        remaining -= interestPayment;

        // 3. Pay Principal last
        var principalPayment = Math.Min(remaining, loan.CurrentPrincipalBalance);

        // Note: In an overpayment scenario, the extra remains in "remaining" 
        // and would typically go to Unearned Revenue or Suspense. 
        // For MVP we assume standard payment.

        return new PaymentSplit(principalPayment, interestPayment, feePayment);
    }

    private record TenantAccounts(
        Guid CashAccountId,
        Guid PrincipalAccountId,
        Guid InterestAccountId,
        Guid FeeAccountId,
        Guid LossExpenseId,
        Guid RecoveryIncomeId);

    private record PaymentSplit(
        decimal PrincipalAmount,
        decimal InterestAmount,
        decimal FeeAmount);
}
