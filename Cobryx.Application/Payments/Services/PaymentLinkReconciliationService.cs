using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Accounting;
using Cobryx.Application.Accounting.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Entities.Lending.Enums;

namespace Cobryx.Application.Payments.Services;

public class PaymentLinkReconciliationService
{
    private readonly ICobryxDbContext _context;
    private readonly FinancialPostingEngine _postingEngine;
    private readonly IStripeService _stripeService;
    private readonly FinancialStateEngine _stateEngine;
    private readonly ILogger<PaymentLinkReconciliationService> _logger;

    public PaymentLinkReconciliationService(
        ICobryxDbContext context,
        FinancialPostingEngine postingEngine,
        IStripeService stripeService,
        FinancialStateEngine stateEngine,
        ILogger<PaymentLinkReconciliationService> logger)
    {
        _context = context;
        _postingEngine = postingEngine;
        _stripeService = stripeService;
        _stateEngine = stateEngine;
        _logger = logger;
    }

    public async Task HandlePaymentSuccessAsync(
        string paymentIntentId,
        Money paidAmount,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Processing successful payment for Intent: {IntentId}", paymentIntentId);

        // 1. Find the PaymentLink
        var link = await _context.PaymentLinks
            .FirstOrDefaultAsync(l => l.StripePaymentIntentId == paymentIntentId, ct);

        if (link == null)
        {
            _logger.LogWarning("PaymentLink not found for Intent: {IntentId}. Reconciliation skipped.", paymentIntentId);
            return;
        }

        if (link.Status == PaymentLinkStatus.Paid)
        {
            _logger.LogInformation("PaymentLink {LinkId} already marked as Paid. Skipping.", link.Id);
            return;
        }

        // 2. Use an explicit transaction for absolute financial integrity (RepeatableRead to prevent race anomalies)
        using var transaction = await _context.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        try
        {
            // 3. Update Link State
            link.MarkAsPaid(paidAmount);

            // 4. Create a formal Payment record
            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.TenantId == link.TenantId && pm.Code == "STRIPE", ct);

            if (paymentMethod == null)
            {
                paymentMethod = await _context.PaymentMethods
                    .FirstOrDefaultAsync(pm => pm.TenantId == link.TenantId, ct);
            }

            var payment = new Payment(
                link.TenantId,
                link.CustomerId,
                paymentMethod?.Id ?? Guid.Empty,
                paidAmount,
                DateTime.UtcNow,
                reference: $"LINK-{link.Id}",
                notes: $"Payment via Payment Link. External Ref: {link.ExternalReference}"
            );

            _context.Payments.Add(payment);

            // 5. Create Ledger Transaction (Deterministic Posting)
            if (link.LoanId.HasValue)
            {
                var loan = await _context.Loans
                    .Include(l => l.Installments)
                    .FirstOrDefaultAsync(l => l.Id == link.LoanId, ct);

                if (loan != null)
                {
                    // BANK-GRADE: Check if loan is ChargedOff for recovery accounting
                    if (loan.FinancialStatus == FinancialStatus.ChargedOff)
                    {
                        await _postingEngine.PostRecoveryAsync(
                            loan,
                            paidAmount.Amount,
                            $"STRIPE-{paymentIntentId}",
                            ct);
                    }
                    else
                    {
                        await _postingEngine.PostLoanPaymentAsync(
                            loan,
                            paidAmount.Amount,
                            $"STRIPE-{paymentIntentId}",
                            ct);
                    }
                }
            }

            // 6. Update Financial State (Inside transaction for absolute atomicity)
            if (link.LoanId.HasValue)
            {
                await _stateEngine.UpdateStatusAsync(link.LoanId.Value, "Payment Received", ct);
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("PaymentLink {LinkId} reconciled successfully. Transaction committed.", link.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to reconcile PaymentLink {LinkId}. Transaction rolled back.", link.Id);
            throw;
        }
    }

    /// <summary>
    /// Processes a refund from Stripe and triggers an atomic Ledger Reversal.
    /// </summary>
    public async Task HandleRefundAsync(
        string paymentIntentId,
        Money refundAmount,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Processing refund for Intent: {IntentId}", paymentIntentId);

        // 1. Find the PaymentLink to get context (Tenant, Loan)
        var link = await _context.PaymentLinks
            .FirstOrDefaultAsync(l => l.StripePaymentIntentId == paymentIntentId, ct);

        using var transaction = await _context.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        try
        {
            var reference = $"PAY-STRIPE-{paymentIntentId}";

            var originalTx = await _context.LedgerTransactions
                .FirstOrDefaultAsync(t => t.ReferenceId == reference && !t.IsReversal, ct);

            if (originalTx == null)
            {
                _logger.LogWarning("Original LedgerTransaction not found for refund (Ref: {Ref}). Reversal skipped.", reference);
                await transaction.RollbackAsync(ct);
                return;
            }

            // 2. Post Reversal
            await _postingEngine.PostReversalAsync(
                originalTx.Id,
                $"Stripe Refund - Amount: {refundAmount.Amount}",
                ct);

            // 3. Update Financial State (Inside transaction)
            if (link?.LoanId != null)
            {
                await _stateEngine.UpdateStatusAsync(link.LoanId.Value, "Refund Processed", ct);
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            _logger.LogInformation("Refund for Intent {IntentId} processed successfully.", paymentIntentId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to process refund for Intent: {IntentId}. Transaction rolled back.", paymentIntentId);
            throw;
        }
    }

    /// <summary>
    /// Self-healing logic: Finds links stuck in 'Processing' and reconciles them against Stripe.
    /// </summary>
    public async Task RecoverStuckProcessingLinksAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - timeout;
        _logger.LogInformation("Starting stuck link recovery for links stuck since: {Cutoff}", cutoff);

        var stuckLinks = await _context.PaymentLinks
            .Where(l => l.Status == PaymentLinkStatus.Processing && l.UpdatedAt < cutoff)
            .ToListAsync(ct);

        if (!stuckLinks.Any())
        {
            _logger.LogInformation("No stuck links found.");
            return;
        }

        foreach (var link in stuckLinks)
        {
            if (string.IsNullOrEmpty(link.StripePaymentIntentId)) continue;

            try
            {
                _logger.LogInformation("Recovering stuck link: {LinkId} (PI: {IntentId})", link.Id, link.StripePaymentIntentId);

                // Bank-Grade: Check real status in Stripe
                var status = await _stripeService.GetPaymentIntentStatusAsync(link.StripePaymentIntentId, ct);

                if (status == "succeeded")
                {
                    _logger.LogInformation("Link {LinkId} was successful in Stripe. Reconciling...", link.Id);

                    // Fetch amount (Stripe amount is in cents)
                    // We'll trust the Link's snapshot amount as the reconciliation target
                    await HandlePaymentSuccessAsync(link.StripePaymentIntentId, link.AmountSnapshot, ct);
                }
                else if (status is "canceled" or "requires_payment_method")
                {
                    _logger.LogWarning("Link {LinkId} failed or was canceled in Stripe. Resetting to Active.", link.Id);

                    // Reset to Active so it can be retried or a new PI generated
                    // This requires a minor state change that we can do outside a heavy transaction
                    // but we'll use a simple SaveChanges
                    link.ResetToActive();
                    await _context.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recover stuck link {LinkId}", link.Id);
            }
        }
    }
}
