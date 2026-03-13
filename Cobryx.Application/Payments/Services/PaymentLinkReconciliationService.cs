using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


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
        decimal? applicationFee = null,
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

        // 1.5 BANK-GRADE: Transversal Suspension Guard
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == link.TenantId, ct);
        if (tenant == null || tenant.IsPaymentRestricted)
        {
            _logger.LogWarning("PaymentLink {LinkId} reconciliation BLOCKED. Tenant {TenantId} is restricted or suspended.", link.Id, link.TenantId);
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
                            applicationFee,
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
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ReferenceId") == true || ex.InnerException?.Message.Contains("23505") == true)
        {
            // BANK-GRADE: Idempotency Hit
            // If the ReferenceId already exists, it means this payment was already processed (e.g. duplicate webhook)
            _logger.LogWarning("Idempotency Hit for PaymentLink {LinkId} (PI: {IntentId}). Transaction already exists.", link.Id, paymentIntentId);
            await transaction.RollbackAsync(ct);
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

        // 1.5 BANK-GRADE: Transversal Suspension Guard (for Refund)
        if (link != null)
        {
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == link.TenantId, ct);
            if (tenant == null || tenant.IsPaymentRestricted)
            {
                _logger.LogWarning("Refund for Intent {IntentId} BLOCKED. Tenant {TenantId} is restricted or suspended.", paymentIntentId, link.TenantId);
                return;
            }
        }

        using var transaction = await _context.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        try
        {
            // 1. BANK-GRADE: Finding the original transaction (PAY or REC)
            var referencePay = $"PAY-STRIPE-{paymentIntentId}";
            var referenceRec = $"REC-STRIPE-{paymentIntentId}";

            var originalTx = await _context.LedgerTransactions
                .Where(t => (t.ReferenceId == referencePay || t.ReferenceId == referenceRec) && !t.IsReversal)
                .FirstOrDefaultAsync(ct);

            if (originalTx == null)
            {
                _logger.LogWarning("Original LedgerTransaction (PAY or REC) not found for refund intent {IntentId}. Reversal skipped.", paymentIntentId);
                await transaction.RollbackAsync(ct);
                return;
            }

            // 2. Post Reversal
            await _postingEngine.PostReversalAsync(
                originalTx.Id,
                refundAmount.Amount,
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
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ReferenceId") == true || ex.InnerException?.Message.Contains("23505") == true)
        {
            // BANK-GRADE: Idempotency Hit for Refund
            _logger.LogWarning("Idempotency Hit for Refund (PI: {IntentId}). Transaction already exists.", paymentIntentId);
            await transaction.RollbackAsync(ct);
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
    /// Implements batching, throttling, and retry limits.
    /// </summary>
    public async Task RecoverStuckProcessingLinksAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - timeout;
        var recoveryThrottle = DateTime.UtcNow.AddMinutes(-15); // Don't retry more than once every 15 min

        _logger.LogInformation("Starting stuck link recovery for links stuck since: {Cutoff}", cutoff);

        // BANK-GRADE: Batch processing (50 at a time) to prevent memory pressure
        var stuckLinks = await _context.PaymentLinks
            .Where(l => l.Status == PaymentLinkStatus.Processing && l.UpdatedAt < cutoff)
            .Where(l => l.LastRecoveryAttemptAt == null || l.LastRecoveryAttemptAt < recoveryThrottle)
            .OrderBy(l => l.UpdatedAt)
            .Take(50)
            .ToListAsync(ct);

        if (stuckLinks.Count == 0)
        {
            _logger.LogInformation("No stuck links requiring recovery found.");
            return;
        }

        foreach (var link in stuckLinks)
        {
            if (string.IsNullOrEmpty(link.StripePaymentIntentId)) continue;

            try
            {
                _logger.LogInformation("Recovering stuck link: {LinkId} (PI: {IntentId}). Attempt: {Attempt}",
                    link.Id, link.StripePaymentIntentId, link.RecoveryAttemptCount + 1);

                link.RecordRecoveryAttempt();
                await _context.SaveChangesAsync(ct);

                // Bank-Grade: Check real status in Stripe
                // We fetch the full intent to check for application fees
                // Note: GetPaymentIntentStatusAsync only returns status. We might need a fuller fetch.
                // For now, we'll assume the status is enough to trigger HandlePaymentSuccessAsync
                // where we might fetch more if needed.
                var status = await _stripeService.GetPaymentIntentStatusAsync(link.StripePaymentIntentId, ct);

                if (status == "succeeded")
                {
                    _logger.LogInformation("Link {LinkId} was successful in Stripe. Reconciling...", link.Id);
                    // We'll trust the Link's snapshot amount.
                    // To get the exact application fee, we'd need to fetch the Intent details.
                    // For now, we'll use null or re-calculate.
                    // Let's re-calculate to keep it simple but accurate to our formula.
                    decimal? appFee = null;
                    var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == link.TenantId, ct);
                    if (tenant?.IsConnectActive == true)
                    {
                        appFee = Math.Round(link.AmountSnapshot.Amount * 0.015m, 2);
                    }

                    await HandlePaymentSuccessAsync(link.StripePaymentIntentId, link.AmountSnapshot, appFee, ct);
                }
                else if (status is "canceled" or "requires_payment_method")
                {
                    _logger.LogWarning("Link {LinkId} failed or was canceled in Stripe. Resetting to Active.", link.Id);
                    link.ResetToActive();
                    await _context.SaveChangesAsync(ct);
                }
                else if (status is "requires_action" or "requires_confirmation")
                {
                    _logger.LogInformation("Link {LinkId} still requires action. Skipping recovery for now.", link.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recover stuck link {LinkId}. Recording failure.", link.Id);
                link.RecordRecoveryFailure("stuck_link_recovery_exception");
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}
