using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Services;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;


namespace Cobryx.Application.Payments.Services
{
    public class PaymentLinkReconciliationService(
        ICobryxDbContext context,
        FinancialPostingEngine postingEngine,
        IStripeService stripeService,
        FinancialStateEngine stateEngine,
        IClock clock,
        ILogger<PaymentLinkReconciliationService> logger)
    {
        public async Task HandlePaymentSuccessAsync(
            string paymentIntentId,
            Money paidAmount,
            decimal? applicationFee = null,
            CancellationToken ct = default)
        {
            logger.LogInformation("Processing successful payment for Intent: {IntentId}", paymentIntentId);

            PaymentLink? link = await context.PaymentLinks
                .FirstOrDefaultAsync(l => l.StripePaymentIntentId == paymentIntentId, ct);

            if (link == null)
            {
                logger.LogWarning("PaymentLink not found for Intent: {IntentId}. Reconciliation skipped.", paymentIntentId);
                return;
            }

            if (link.Status == PaymentLinkStatus.Paid)
            {
                logger.LogInformation("PaymentLink {LinkId} already marked as Paid. Skipping.", link.Id);
                return;
            }

            Tenant? tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == link.TenantId, ct);
            if (tenant == null || tenant.IsPaymentRestricted)
            {
                logger.LogWarning("PaymentLink {LinkId} reconciliation BLOCKED. Tenant {TenantId} is restricted or suspended.", link.Id, link.TenantId);
                return;
            }

            await using IDbContextTransaction transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
            try
            {
                link.MarkAsPaid(paidAmount);

                PaymentMethod? paymentMethod = await context.PaymentMethods
                    .FirstOrDefaultAsync(pm => pm.TenantId == link.TenantId && pm.Code == "STRIPE", ct);

                paymentMethod ??= await context.PaymentMethods
                        .FirstOrDefaultAsync(pm => pm.TenantId == link.TenantId, ct);

                var payment = new Payment(
                    link.TenantId,
                    link.CustomerId,
                    paymentMethod?.Id ?? Guid.Empty,
                    paidAmount,
                    clock.UtcNow,
                    reference: $"LINK-{link.Id}",
                    notes: $"Payment via Payment Link. External Ref: {link.ExternalReference}"
                );

                _ = context.Payments.Add(payment);

                if (link.LoanId.HasValue)
                {
                    Loan? loan = await context.Loans
                        .Include(l => l.Installments)
                        .FirstOrDefaultAsync(l => l.Id == link.LoanId, ct);

                    if (loan != null)
                    {
                        _ = loan.FinancialStatus == FinancialStatus.ChargedOff
                            ? await postingEngine.PostRecoveryAsync(
                                loan,
                                paidAmount.Amount,
                                $"STRIPE-{paymentIntentId}",
                                ct)
                            : await postingEngine.PostLoanPaymentAsync(
                                loan,
                                paidAmount.Amount,
                                $"STRIPE-{paymentIntentId}",
                                applicationFee,
                                ct);
                    }
                }

                if (link.LoanId.HasValue)
                {
                    await stateEngine.UpdateStatusAsync(link.LoanId.Value, "Payment Received", ct);
                }

                _ = await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                logger.LogInformation("PaymentLink {LinkId} reconciled successfully. Transaction committed.", link.Id);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ReferenceId") == true || ex.InnerException?.Message.Contains("23505") == true)
            {
                logger.LogWarning("Idempotency Hit for PaymentLink {LinkId} (PI: {IntentId}). Transaction already exists.", link.Id, paymentIntentId);
                await transaction.RollbackAsync(ct);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                logger.LogError(ex, "Failed to reconcile PaymentLink {LinkId}. Transaction rolled back.", link.Id);
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
            logger.LogInformation("Processing refund for Intent: {IntentId}", paymentIntentId);

            PaymentLink? link = await context.PaymentLinks
                .FirstOrDefaultAsync(l => l.StripePaymentIntentId == paymentIntentId, ct);

            if (link != null)
            {
                Tenant? tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == link.TenantId, ct);
                if (tenant == null || tenant.IsPaymentRestricted)
                {
                    logger.LogWarning("Refund for Intent {IntentId} BLOCKED. Tenant {TenantId} is restricted or suspended.", paymentIntentId, link.TenantId);
                    return;
                }
            }

            await using IDbContextTransaction transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
            try
            {
                var referencePay = $"PAY-STRIPE-{paymentIntentId}";
                var referenceRec = $"REC-STRIPE-{paymentIntentId}";

                LedgerTransaction? originalTx = await context.LedgerTransactions
                    .Where(t => (t.ReferenceId == referencePay || t.ReferenceId == referenceRec) && !t.IsReversal)
                    .FirstOrDefaultAsync(ct);

                if (originalTx == null)
                {
                    logger.LogWarning("Original LedgerTransaction (PAY or REC) not found for refund intent {IntentId}. Reversal skipped.", paymentIntentId);
                    await transaction.RollbackAsync(ct);
                    return;
                }

                _ = await postingEngine.PostReversalAsync(
                    originalTx.Id,
                    refundAmount.Amount,
                    $"Stripe Refund - Amount: {refundAmount.Amount}",
                    ct);

                if (link?.LoanId != null)
                {
                    await stateEngine.UpdateStatusAsync(link.LoanId.Value, "Refund Processed", ct);
                }

                _ = await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                logger.LogInformation("Refund for Intent {IntentId} processed successfully.", paymentIntentId);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ReferenceId") == true || ex.InnerException?.Message.Contains("23505") == true)
            {
                logger.LogWarning("Idempotency Hit for Refund (PI: {IntentId}). Transaction already exists.", paymentIntentId);
                await transaction.RollbackAsync(ct);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                logger.LogError(ex, "Failed to process refund for Intent: {IntentId}. Transaction rolled back.", paymentIntentId);
                throw;
            }
        }

        /// <summary>
        /// Self-healing logic: Finds links stuck in 'Processing' and reconciles them against Stripe.
        /// Implements batching, throttling, and retry limits.
        /// </summary>
        public async Task RecoverStuckProcessingLinksAsync(TimeSpan timeout, CancellationToken ct = default)
        {
            DateTime cutoff = clock.UtcNow - timeout;
            DateTime recoveryThrottle = clock.UtcNow.AddMinutes(-15);

            logger.LogInformation("Starting stuck link recovery for links stuck since: {Cutoff}", cutoff);

            List<PaymentLink> stuckLinks = await context.PaymentLinks
                .Where(l => l.Status == PaymentLinkStatus.Processing && l.UpdatedAt < cutoff)
                .Where(l => l.LastRecoveryAttemptAt == null || l.LastRecoveryAttemptAt < recoveryThrottle)
                .OrderBy(l => l.UpdatedAt)
                .Take(50)
                .ToListAsync(ct);

            if (stuckLinks.Count == 0)
            {
                logger.LogInformation("No stuck links requiring recovery found.");
                return;
            }

            foreach (PaymentLink link in stuckLinks)
            {
                if (string.IsNullOrEmpty(link.StripePaymentIntentId))
                {
                    continue;
                }

                try
                {
                    logger.LogInformation("Recovering stuck link: {LinkId} (PI: {IntentId}). Attempt: {Attempt}",
                        link.Id, link.StripePaymentIntentId, link.RecoveryAttemptCount + 1);

                    link.RecordRecoveryAttempt(clock.UtcNow);
                    _ = await context.SaveChangesAsync(ct);

                    var status = await stripeService.GetPaymentIntentStatusAsync(link.StripePaymentIntentId, ct);

                    if (status == StripeConstants.PaymentIntentStatuses.Succeeded)
                    {
                        logger.LogInformation("Link {LinkId} was successful in Stripe. Reconciling...", link.Id);
                        decimal? appFee = null;
                        Tenant? tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == link.TenantId, ct);
                        if (tenant?.IsConnectActive == true)
                        {
                            appFee = Math.Round(link.AmountSnapshot.Amount * 0.015m, 2);
                        }

                        await HandlePaymentSuccessAsync(link.StripePaymentIntentId, link.AmountSnapshot, appFee, ct);
                    }
                    else if (status is StripeConstants.PaymentIntentStatuses.Canceled or StripeConstants.PaymentIntentStatuses.RequiresPaymentMethod)
                    {
                        logger.LogWarning("Link {LinkId} failed or was canceled in Stripe. Resetting to Active.", link.Id);
                        link.ResetToActive();
                        _ = await context.SaveChangesAsync(ct);
                    }
                    else if (status is StripeConstants.PaymentIntentStatuses.RequiresAction or StripeConstants.PaymentIntentStatuses.RequiresConfirmation)
                    {
                        logger.LogInformation("Link {LinkId} still requires action. Skipping recovery for now.", link.Id);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to recover stuck link {LinkId}. Recording failure.", link.Id);
                    link.RecordRecoveryFailure("stuck_link_recovery_exception", clock.UtcNow);
                    _ = await context.SaveChangesAsync(ct);
                }
            }
        }
    }
}
