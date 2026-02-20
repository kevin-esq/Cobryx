using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Accounting;
using Cobryx.Application.Accounting.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Payments.Services;

public class PaymentLinkReconciliationService
{
    private readonly ICobryxDbContext _context;
    private readonly FinancialPostingEngine _postingEngine;
    private readonly ILogger<PaymentLinkReconciliationService> _logger;

    public PaymentLinkReconciliationService(
        ICobryxDbContext context, 
        FinancialPostingEngine postingEngine,
        ILogger<PaymentLinkReconciliationService> logger)
    {
        _context = context;
        _postingEngine = postingEngine;
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

        // 2. Use a transaction for financial integrity
        // Note: DbContext in Cobryx handles orchestration; we perform the domain-mark and record-creation.
        
        // 3. Update Link State
        link.MarkAsPaid(paidAmount);

        // 4. Create a formal Payment record
        // Find a default 'Stripe' payment method for the tenant or create a placeholder logic
        var paymentMethod = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.TenantId == link.TenantId && pm.Code == "STRIPE", ct);

        if (paymentMethod == null)
        {
             // Fallback or create auto-method logic
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

        // Link to Loan if applicable
        if (link.LoanId.HasValue)
        {
             // Future: Link more deeply to loan installments via PaymentAllocation
             // For now, we register the payment against the customer/tenant.
        }

        _context.Payments.Add(payment);
        
        // 5. Create Ledger Transaction (Deterministic Posting)
        if (link.LoanId.HasValue)
        {
            var loan = await _context.Loans
                .Include(l => l.Installments)
                .FirstOrDefaultAsync(l => l.Id == link.LoanId, ct);

            if (loan != null)
            {
                await _postingEngine.PostLoanPaymentAsync(
                    loan, 
                    paidAmount.Amount, 
                    $"STRIPE-{paymentIntentId}", 
                    ct);
            }
        }
        
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("PaymentLink {LinkId} reconciled successfully. Payment {PaymentId} recorded.", link.Id, payment.Id);
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

        // Find the original LedgerTransaction for this payment
        // We use the reference pattern "STRIPE-{paymentIntentId}" defined in PostLoanPayment
        var reference = $"STRIPE-{paymentIntentId}";
        
        var originalTx = await _context.LedgerTransactions
            .FirstOrDefaultAsync(t => t.ReferenceId == reference && !t.IsReversal, ct);

        if (originalTx == null)
        {
            _logger.LogWarning("Original LedgerTransaction not found for refund of Intent: {IntentId}. Reversal skipped.", paymentIntentId);
            return;
        }

        await _postingEngine.PostReversalAsync(
            originalTx.Id, 
            $"Stripe Refund - Amount: {refundAmount.Amount}", 
            ct);

        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Refund processed and Ledger Reversal posted successfully for Intent: {IntentId}.", paymentIntentId);
    }
}
