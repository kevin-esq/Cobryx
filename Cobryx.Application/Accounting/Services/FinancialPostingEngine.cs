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
            loan.Id.ToString());

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
        
        _logger.LogInformation(
            "Financial Posting Finished: Principal: {P}, Interest: {I}, Fees: {F}", 
            split.PrincipalAmount, split.InterestAmount, split.FeeAmount);

        return transaction.Id;
    }

    /// <summary>
    /// Creates a Mirror Reversal for an existing transaction.
    /// Used for Refunds, Chargebacks, and Storno entries.
    /// </summary>
    public async Task<Guid> PostReversalAsync(Guid originalTransactionId, string reason, CancellationToken ct = default)
    {
        var original = await _context.LedgerTransactions
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == originalTransactionId, ct);

        if (original == null)
            throw new DomainException(DomainErrorCode.Common.GeneralError);

        var reversal = LedgerTransaction.CreateReversal(original, $"REVERSAL: {reason}");
        
        _context.LedgerTransactions.Add(reversal);
        
        _logger.LogInformation("Posted Mirror Reversal for Transaction {OriginalId}", originalTransactionId);
        
        return reversal.Id;
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
            FeeAccountId: accounts.First(a => a.Code == "4020").Id
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
        Guid FeeAccountId);

    private record PaymentSplit(
        decimal PrincipalAmount, 
        decimal InterestAmount, 
        decimal FeeAmount);
}
