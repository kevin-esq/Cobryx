using System.Collections.Generic;
using System.Linq;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Entities.Accounting;
using Cobryx.Domain.Entities.Accounting.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cobryx.Application.Accounting.Services;

public class ReconciliationEngine
{
    private readonly ICobryxDbContext _dbContext;
    private readonly IStripeService _stripeService;
    private readonly FinancialPostingEngine _postingEngine;
    private readonly CobryxMetrics _metrics;
    private readonly ILogger<ReconciliationEngine> _logger;

    // Institutional timing tolerance: 15 minutes lag for webhooks
    private static readonly TimeSpan _timingTolerance = TimeSpan.FromMinutes(15);

    public ReconciliationEngine(
        ICobryxDbContext dbContext,
        IStripeService stripeService,
        FinancialPostingEngine postingEngine,
        CobryxMetrics metrics,
        ILogger<ReconciliationEngine> _logger)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
        _postingEngine = postingEngine;
        _metrics = metrics;
        this._logger = _logger;
    }

    public async Task<ReconciliationAudit> ReconcileAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        string? stripeAccountId = null,
        CancellationToken ct = default)
    {
        var runId = Guid.NewGuid();
        _logger.LogInformation("Starting Stripe-grade Reconciliation Run {RunId} for Tenant {TenantId}", runId, tenantId);

        // 1. Fetch Balances
        var (available, pending) = await _stripeService.GetBalanceAsync(stripeAccountId, ct);
        var ledgerBalance = await GetLedgerBalanceAsync(tenantId, ct);

        // 2. Windowed Scan of PaymentIntents
        var drifts = new List<DriftDetail>();
        var totalDriftsManaged = 0;
        var totalRepaired = 0;
        string? lastCursor = null;
        bool hasMore = true;

        while (hasMore)
        {
            var intents = await _stripeService.ListPaymentIntentsAsync(from, to, stripeAccountId, lastCursor, ct);
            if (!intents.Any()) break;

            foreach (var intent in intents.Where(i => i.Status == "succeeded"))
            {
                var drift = await AnalyzeIntentAsync(intent, tenantId, ct);
                if (drift != null)
                {
                    totalDriftsManaged++;

                    // MULTI-PASS: Check if this drift was already detected in the previous run
                    var isConfirmed = await IsDriftConfirmedAsync(tenantId, intent.Id, ct);
                    if (isConfirmed)
                    {
                        drift = drift with { Status = ReconciliationStatus.ConfirmedDrift };
                    }

                    // AUTO-HEALING: Attempt repair for high-confidence confirmed drifts
                    if (drift.Type == DriftType.MissingPayment && isConfirmed)
                    {
                        var repaired = await AutoRepairDriftAsync(intent, tenantId, ct);
                        if (repaired)
                        {
                            totalRepaired++;
                            _metrics.ReconciliationAutoRepairedTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
                            drift = drift with { Type = DriftType.None, Message = drift.Message + " [AUTO-REPAIRED]" };
                            _logger.LogInformation("Drift {Id} auto-repaired successfully after confirmation.", intent.Id);
                        }
                    }

                    if (drift.Type != DriftType.None)
                    {
                        drifts.Add(drift);
                        _metrics.ReconciliationDriftTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()), new KeyValuePair<string, object?>("type", drift.Type.ToString()));
                    }
                }
            }

            lastCursor = intents.Last().Id;
            hasMore = intents.Count == 100;
        }

        // 3. Deep Settlement Scan (BalanceTransactions)
        var settlementDrifts = await ReconcileSettlementAsync(tenantId, from, to, stripeAccountId, ct);
        foreach (var sd in settlementDrifts)
        {
            drifts.Add(sd);
            _metrics.SettlementDriftTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()), new KeyValuePair<string, object?>("type", sd.Type.ToString()));

            if (sd.Type == DriftType.AmountMismatch)
            {
                _metrics.FeeMismatchTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
            }
        }

        // 3. Summarize Run
        var severity = ReconciliationSeverity.Info;
        var status = ReconciliationStatus.Synced;

        if (drifts.Any())
        {
            status = ReconciliationStatus.SoftDrift;
            severity = ReconciliationSeverity.Info;
        }
        else if (totalRepaired > 0 && totalRepaired == totalDriftsManaged)
        {
            status = ReconciliationStatus.Repaired;
            severity = ReconciliationSeverity.Info;
        }
        else if (totalRepaired > 0)
        {
            status = ReconciliationStatus.SoftDrift;
            severity = ReconciliationSeverity.Warning;
        }

        if (drifts.Any(d => d.Severity == ReconciliationSeverity.Critical))
        {
            severity = ReconciliationSeverity.Critical;
            status = ReconciliationStatus.HardDrift;
        }
        else if (drifts.Any(d => d.Severity == ReconciliationSeverity.Error))
        {
            severity = ReconciliationSeverity.Error;
            status = ReconciliationStatus.HardDrift;
        }
        else if (drifts.Any(d => d.Severity == ReconciliationSeverity.Warning))
        {
            severity = ReconciliationSeverity.Warning;
            status = ReconciliationStatus.SoftDrift;
        }

        var audit = new ReconciliationAudit(
            tenantId,
            runId,
            from,
            to,
            ledgerBalance,
            available,
            pending,
            status,
            severity,
            drifts.Count,
            JsonConvert.SerializeObject(drifts, new StringEnumConverter()),
            lastCursor
        );

        _dbContext.ReconciliationAudits.Add(audit);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Reconciliation Run {RunId} completed with status {Status}", runId, audit.Status);
        return audit;
    }

    private async Task<decimal> GetLedgerBalanceAsync(Guid tenantId, CancellationToken ct)
    {
        var cashAccount = await _dbContext.LedgerAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code == "1010", ct);

        if (cashAccount == null) return 0;

        return await _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(e => e.AccountId == cashAccount.Id)
            .SumAsync(e => e.Debit - e.Credit, ct);
    }

    private async Task<DriftDetail?> AnalyzeIntentAsync(StripePaymentIntentDto intent, Guid tenantId, CancellationToken ct)
    {
        // 1. Check if recorded in Ledger via ReferenceId (PAY-STRIPE-{PI} or REC-STRIPE-{PI})
        var referencePay = $"PAY-STRIPE-{intent.Id}";
        var referenceRec = $"REC-STRIPE-{intent.Id}";

        var ledgerExists = await _dbContext.LedgerTransactions
            .AsNoTracking()
            .AnyAsync(t => t.TenantId == tenantId && (t.ReferenceId == referencePay || t.ReferenceId == referenceRec), ct);

        if (ledgerExists) return null; // In-sync

        // 2. Apply Timing Tolerance (Soft Drift if < 15 mins)
        var age = DateTime.UtcNow - intent.Created;
        if (age < _timingTolerance)
        {
            return new DriftDetail(
                intent.Id,
                DriftType.TimingLag,
                ReconciliationSeverity.Info,
                $"Payment succeeded very recently ({Math.Round(age.TotalMinutes, 1)}m ago); webhook/processing might be in transit.",
                "webhook_delay"
            );
        }

        // 3. Hard Discrepancy: Missing Payment in Ledger
        return new DriftDetail(
            intent.Id,
            DriftType.MissingPayment,
            ReconciliationSeverity.Error,
            $"Payment ({intent.Amount / 100m} {intent.Currency.ToUpper()}) succeeded in Stripe but is missing from Ledger after 15m.",
            "missing_webhook_or_processing_failure"
        );
    }

    private async Task<List<DriftDetail>> ReconcileSettlementAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        string? stripeAccountId = null,
        CancellationToken ct = default)
    {
        var settlementDrifts = new List<DriftDetail>();
        string? lastCursor = null;
        bool hasMore = true;

        while (hasMore)
        {
            var transactions = await _stripeService.ListBalanceTransactionsAsync(from, to, stripeAccountId, lastCursor, ct);
            if (!transactions.Any()) break;

            foreach (var tx in transactions)
            {
                var drift = await AnalyzeBalanceTransactionAsync(tx, tenantId, ct);
                if (drift != null)
                {
                    settlementDrifts.Add(drift);
                }
            }

            lastCursor = transactions.Last().Id;
            hasMore = transactions.Count == 100;
        }

        return settlementDrifts;
    }

    private async Task<DriftDetail?> AnalyzeBalanceTransactionAsync(StripeBalanceTransactionDto tx, Guid tenantId, CancellationToken ct)
    {
        // 1. Map Stripe Types to Ledger Reference Prefixes
        // payout -> PAYOUT-STRIPE-{ID}
        // refund -> REF-STRIPE-{ID}
        // stripe_fee -> FEE-STRIPE-{BT_ID}

        string? referenceId = tx.Type switch
        {
            "payout" => $"PAYOUT-STRIPE-{tx.SourceId ?? tx.Id}",
            "refund" => $"REF-STRIPE-{tx.SourceId ?? tx.Id}",
            "stripe_fee" => $"FEE-STRIPE-{tx.Id}",
            _ => null
        };

        if (referenceId == null) return null; // Only interested in institutional movements

        // 2. Check Ledger Existence
        var ledgerTx = await _dbContext.LedgerTransactions
            .AsNoTracking()
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.ReferenceId == referenceId, ct);

        if (ledgerTx == null)
        {
            // Apply 15m tolerance for settlement too (soft drift)
            var age = DateTime.UtcNow - tx.Created;
            if (age < _timingTolerance)
            {
                return new DriftDetail(tx.Id, DriftType.TimingLag, ReconciliationSeverity.Info, $"Settlement {tx.Type} recently created. Webhook in transit.", "settlement_lag", ReconciliationStatus.SoftDrift);
            }

            return new DriftDetail(tx.Id, DriftType.MissingPayment, ReconciliationSeverity.Error, $"Settlement {tx.Type} ({tx.Amount / 100m} {tx.Currency.ToUpper()}) missing from Ledger.", "missing_settlement", ReconciliationStatus.HardDrift);
        }

        // 3. Validate Institutional Fields (Deep Pass)
        var ledgerAmount = ledgerTx.Entries.Sum(e => e.Debit - e.Credit); // Simplified for simple movements
        // For BTs, the Net amount is what landed/left the account
        var stripeNet = tx.Net / 100m;

        // In Cobryx, movements are recorded with their absolute impact.
        // We'll use a tolerance of 0.01 for rounding.
        if (Math.Abs(Math.Abs(stripeNet) - Math.Abs(ledgerAmount)) > 0.01m)
        {
            return new DriftDetail(tx.Id, DriftType.AmountMismatch, ReconciliationSeverity.Critical, $"Settlement Amount Mismatch: Stripe Net={stripeNet}, Ledger={ledgerAmount}", "amount_inconsistency", ReconciliationStatus.HardDrift);
        }

        return null;
    }

    private async Task<bool> IsDriftConfirmedAsync(Guid tenantId, string externalId, CancellationToken ct)
    {
        // Fetch the most recent audit for this tenant
        var lastAudit = await _dbContext.ReconciliationAudits
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (lastAudit == null || string.IsNullOrEmpty(lastAudit.DriftDetailsJson))
            return false;

        // Simple check: was this ExternalId in the last report?
        return lastAudit.DriftDetailsJson.Contains(externalId);
    }

    private async Task<bool> AutoRepairDriftAsync(StripePaymentIntentDto intent, Guid tenantId, CancellationToken ct)
    {
        try
        {
            // 1. Check if it's a Loan Payment (Metadata contains LoanId)
            if (intent.Metadata.TryGetValue("LoanId", out var loanIdStr) && Guid.TryParse(loanIdStr, out var loanId))
            {
                var loan = await _dbContext.Loans
                    .FirstOrDefaultAsync(l => l.Id == loanId && l.TenantId == tenantId, ct);

                if (loan != null)
                {
                    _logger.LogInformation("Auto-Repair: Re-posting missing payment {IntentId} for Loan {LoanId}", intent.Id, loanId);

                    var amount = intent.Amount / 100m;
                    await _postingEngine.PostLoanPaymentAsync(loan, amount, $"STRIPE-{intent.Id}", null, ct);

                    await _dbContext.SaveChangesAsync(ct);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-Repair failed for Intent {IntentId}", intent.Id);
            return false;
        }
    }

    private record DriftDetail(
        string ExternalId,
        DriftType Type,
        ReconciliationSeverity Severity,
        string Message,
        string? RootCause = null,
        ReconciliationStatus Status = ReconciliationStatus.HardDrift);
}
