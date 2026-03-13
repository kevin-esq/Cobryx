using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Messaging;
using Cobryx.Domain.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services;

/// <summary>
/// The "Brain" of the financial system. Orchestrates how payments are recorded in the Ledger.
/// Enforces deterministic splits and atomic journal creation.
/// </summary>
public class FinancialPostingEngine(ICobryxDbContext context, ILogger<FinancialPostingEngine> logger)
{
    private readonly ICobryxDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<FinancialPostingEngine> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Guid> PostLoanPaymentAllocationAsync(
        Loan loan,
        LoanPaymentAllocation allocation,
        string reference,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Posting payment allocation for Loan {LoanId}, Payment {PaymentId}", loan.Id, allocation.PaymentId);

        var accounts = await GetTenantSystemAccountsAsync(loan.TenantId, ct);

        var transaction = new LedgerTransaction(
            loan.TenantId,
            $"Loan Payment - {reference}",
            $"PAY-{reference}",
            loan.Id);

        var totalAmount = allocation.PrincipalApplied + allocation.InterestApplied + allocation.FeesApplied;

        transaction.AddEntry(accounts.CashAccountId, totalAmount, 0);

        if (allocation.PrincipalApplied > 0)
            transaction.AddEntry(accounts.PrincipalAccountId, 0, allocation.PrincipalApplied);

        if (allocation.InterestApplied > 0)
            transaction.AddEntry(accounts.InterestAccountId, 0, allocation.InterestApplied);

        if (allocation.FeesApplied > 0)
            transaction.AddEntry(accounts.FeeAccountId, 0, allocation.FeesApplied);

        transaction.Post();
        _context.LedgerTransactions.Add(transaction);

        var payload = JsonSerializer.Serialize(new
        {
            allocation.PaymentId,
            allocation.LoanId,
            TotalApplied = totalAmount,
            Principal = allocation.PrincipalApplied,
            Interest = allocation.InterestApplied,
            Fees = allocation.FeesApplied
        });

        await SaveWithSemanticOutboxAsync(transaction, FinancialEventType.PaymentPosted, loan.Id, payload, ct);

        return transaction.Id;
    }

    public async Task<Guid> PostLoanPaymentAsync(
        Loan loan,
        decimal amount,
        string reference,
        decimal? platformFee = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Posting payment of {Amount} for Loan {LoanId}", amount, loan.Id);

        var accounts = await GetTenantSystemAccountsAsync(loan.TenantId, ct);
        var split = CalculatePaymentSplit(loan, amount);

        var transaction = new LedgerTransaction(
            loan.TenantId,
            $"Loan Payment - {reference}",
            $"PAY-{reference}",
            loan.Id);

        transaction.AddEntry(accounts.CashAccountId, amount, 0);

        if (split.PrincipalAmount > 0)
            transaction.AddEntry(accounts.PrincipalAccountId, 0, split.PrincipalAmount);

        if (split.InterestAmount > 0)
            transaction.AddEntry(accounts.InterestAccountId, 0, split.InterestAmount);

        if (split.FeeAmount > 0)
            transaction.AddEntry(accounts.FeeAccountId, 0, split.FeeAmount);

        transaction.Post();
        _context.LedgerTransactions.Add(transaction);

        if (platformFee is > 0)
        {
            var platformAccounts = await GetTenantSystemAccountsAsync(CobryxDefaults.PlatformTenantId, ct);
            var platformTx = new LedgerTransaction(
                CobryxDefaults.PlatformTenantId,
                $"Platform Fee - {reference} (Tenant: {loan.TenantId})",
                $"FEE-{reference}");

            platformTx.AddEntry(platformAccounts.CashAccountId, platformFee.Value, 0);
            platformTx.AddEntry(platformAccounts.FeeAccountId, 0, platformFee.Value);
            platformTx.Post();
            _context.LedgerTransactions.Add(platformTx);

            await SaveWithSemanticOutboxAsync(platformTx, FinancialEventType.LateFeeApplied, loan.Id,
                JsonSerializer.Serialize(new { PlatformFee = platformFee }), ct);
        }

        await SaveWithSemanticOutboxAsync(transaction, FinancialEventType.PaymentPosted, loan.Id,
            JsonSerializer.Serialize(split), ct);

        return transaction.Id;
    }

    public async Task<Guid> PostReversalAsync(Guid originalTransactionId, decimal amount, string reason, CancellationToken ct = default)
    {
        var original = await _context.LedgerTransactions
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == originalTransactionId, ct)
            ?? throw new DomainException(DomainErrorCode.Common.GeneralError);

        var existingReversals = await _context.LedgerTransactions
            .Where(t => t.OriginalTransactionId == originalTransactionId && t.IsPosted)
            .Include(t => t.Entries)
            .ToListAsync(ct);

        var alreadyReversed = existingReversals.SelectMany(static t => t.Entries).Sum(static e => Math.Abs(e.Debit));
        var originalTotal = original.Entries.Sum(e => Math.Abs(e.Debit));
        var remainingReversibleAmount = Math.Max(0, originalTotal - alreadyReversed);

        if (amount > remainingReversibleAmount + 0.01m)
        {
            throw new DomainException(DomainErrorCode.Common.GeneralError);
        }

        var reversal = LedgerTransaction.CreatePartialReversal(original, amount, $"REVERSAL: {reason}");
        _context.LedgerTransactions.Add(reversal);

        if (original.ReferenceId?.StartsWith("PAY-") == true)
        {
            var feeRef = original.ReferenceId.Replace("PAY-", "FEE-");
            var platformTx = await _context.LedgerTransactions
                .Include(t => t.Entries)
                .FirstOrDefaultAsync(t => t.ReferenceId == feeRef && t.TenantId == CobryxDefaults.PlatformTenantId, ct);

            if (platformTx != null)
            {
                var platformTotal = platformTx.Entries.Sum(e => Math.Abs(e.Debit));
                var ratio = amount / originalTotal;
                var platformRefundAmount = Math.Round(platformTotal * ratio, 2);

                var platformReversal = LedgerTransaction.CreatePartialReversal(platformTx, platformRefundAmount, $"REVERSAL (FEE): {reason}");
                _context.LedgerTransactions.Add(platformReversal);

                await SaveWithSemanticOutboxAsync(platformReversal, FinancialEventType.ReversalPosted, original.LoanId ?? Guid.Empty,
                    JsonSerializer.Serialize(new { OriginalTransactionId = originalTransactionId, ReversalAmount = platformRefundAmount, Reason = reason }), ct);
            }
        }

        await SaveWithSemanticOutboxAsync(reversal, FinancialEventType.ReversalPosted, original.LoanId ?? Guid.Empty,
            JsonSerializer.Serialize(new { OriginalTransactionId = originalTransactionId, ReversalAmount = amount, Reason = reason }), ct);

        return reversal.Id;
    }

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

        transaction.AddEntry(accounts.LossExpenseId, totalOutstanding, 0);
        transaction.AddEntry(accounts.PrincipalAccountId, 0, loan.CurrentPrincipalBalance);
        if (loan.CurrentInterestBalance > 0)
            transaction.AddEntry(accounts.InterestAccountId, 0, loan.CurrentInterestBalance);
        if (loan.CurrentLateFeeBalance > 0)
            transaction.AddEntry(accounts.FeeAccountId, 0, loan.CurrentLateFeeBalance);

        transaction.Post();
        _context.LedgerTransactions.Add(transaction);

        await SaveWithSemanticOutboxAsync(transaction, FinancialEventType.LoanWriteOff, loan.Id,
            JsonSerializer.Serialize(new { TotalOutstanding = totalOutstanding, Reason = reason }), ct);

        return transaction.Id;
    }

    public async Task<Guid> PostRecoveryAsync(Loan loan, decimal amount, string reference, CancellationToken ct = default)
    {
        var accounts = await GetTenantSystemAccountsAsync(loan.TenantId, ct);

        var transaction = new LedgerTransaction(
            loan.TenantId,
            $"RECOVERY: {reference}",
            $"REC-{reference}",
            loan.Id);

        transaction.AddEntry(accounts.CashAccountId, amount, 0);
        transaction.AddEntry(accounts.RecoveryIncomeId, 0, amount);

        transaction.Post();
        _context.LedgerTransactions.Add(transaction);

        await SaveWithSemanticOutboxAsync(transaction, FinancialEventType.RecoveryPayment, loan.Id,
            JsonSerializer.Serialize(new { Amount = amount, Reference = reference }), ct);

        return transaction.Id;
    }

    private async Task SaveWithSemanticOutboxAsync(
        LedgerTransaction transaction,
        FinancialEventType eventType,
        Guid entityId,
        string payloadJson,
        CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
        var sequenceId = transaction.Entries.OrderBy(e => e.JournalSequenceId).First().JournalSequenceId;

        var outboxMessage = new OutboxMessage(
            transaction.TenantId,
            eventType.ToString(),
            payloadJson,
            entityId,
            sequenceId,
            transaction.TenantId.ToString(),
            null
        );

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(ct);
    }

    private async Task<TenantAccounts> GetTenantSystemAccountsAsync(Guid tenantId, CancellationToken ct)
    {
        var accounts = await _context.LedgerAccounts
            .Where(a => a.TenantId == tenantId && a.IsSystem)
            .ToListAsync(ct);

        return new TenantAccounts(
            CashAccountId: accounts.First(a => a.Code == "1010").Id,
            PrincipalAccountId: accounts.First(a => a.Code == "1210").Id,
            InterestAccountId: accounts.First(a => a.Code == "4010").Id,
            FeeAccountId: accounts.First(a => a.Code == "4020").Id,
            LossExpenseId: accounts.First(a => a.Code == "5010").Id,
            RecoveryIncomeId: accounts.First(a => a.Code == "4030").Id
        );
    }

    private static PaymentSplit CalculatePaymentSplit(Loan loan, decimal amount)
    {
        var remaining = amount;
        var feePayment = Math.Min(remaining, loan.CurrentLateFeeBalance);
        remaining -= feePayment;
        var interestPayment = Math.Min(remaining, loan.CurrentInterestBalance);
        remaining -= interestPayment;
        var principalPayment = Math.Min(remaining, loan.CurrentPrincipalBalance);

        return new PaymentSplit(principalPayment, interestPayment, feePayment);
    }

    public record TenantAccounts(
        Guid CashAccountId,
        Guid PrincipalAccountId,
        Guid InterestAccountId,
        Guid FeeAccountId,
        Guid LossExpenseId,
        Guid RecoveryIncomeId);

    public record PaymentSplit(
        decimal PrincipalAmount,
        decimal InterestAmount,
        decimal FeeAmount);
}
