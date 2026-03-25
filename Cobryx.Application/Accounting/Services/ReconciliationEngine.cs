using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Lending;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cobryx.Application.Accounting.Services
{
    public class ReconciliationEngine(
        ICobryxDbContext dbContext,
        IStripeService stripeService,
        FinancialPostingEngine postingEngine,
        CobryxMetrics metrics,
        ILogger<ReconciliationEngine> logger)
    {
        private static readonly TimeSpan _timingTolerance = TimeSpan.FromMinutes(15);

        public async Task<ReconciliationAudit> ReconcileAsync(
            Guid tenantId,
            DateTime from,
            DateTime to,
            string? stripeAccountId = null,
            CancellationToken ct = default)
        {
            var runId = Guid.NewGuid();
            logger.LogInformation("Starting reconciliation run {RunId} for tenant {TenantId}", runId, tenantId);

            var (available, pending) = await stripeService.GetBalanceAsync(stripeAccountId, ct);
            var ledgerBalance = await GetLedgerBalanceAsync(tenantId, ct);

            var drifts = new List<DriftDetail>();
            ReconciliationStatus status = ReconciliationStatus.Synced;

            IntentReconciliationResult result =
                await ReconcilePaymentIntentsAsync(tenantId, from, to, stripeAccountId, drifts, status, ct);
            var lastCursor = result.LastCursor;
            var totalDriftsManaged = result.DriftsManaged;
            var totalRepaired = result.Repaired;
            status = result.Status;
            List<DriftDetail> settlementDrifts =
                await ReconcileSettlementAsync(tenantId, from, to, stripeAccountId, ct);
            foreach (DriftDetail sd in settlementDrifts)
            {
                drifts.Add(sd);
                metrics.SettlementDriftTotal.Add(1,
                    new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()),
                    new KeyValuePair<string, object?>("type", sd.Type.ToString()));

                if (sd.Type == DriftType.AmountMismatch)
                {
                    metrics.FeeMismatchTotal.Add(1,
                        new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
                }
            }

            (status, ReconciliationSeverity severity) =
                ResolveStatusAndSeverity(drifts, totalDriftsManaged, totalRepaired, status);

            var audit = new ReconciliationAudit(
                tenantId, runId, from, to,
                ledgerBalance, available, pending,
                status, severity,
                drifts.Count,
                JsonConvert.SerializeObject(drifts, new StringEnumConverter()),
                null,
                lastCursor);

            _ = dbContext.ReconciliationAudits.Add(audit);
            _ = await dbContext.SaveChangesAsync(ct);

            logger.LogInformation("Reconciliation run {RunId} completed with status {Status}", runId, audit.Status);
            return audit;
        }

        private record IntentReconciliationResult(
            string? LastCursor,
            int DriftsManaged,
            int Repaired,
            ReconciliationStatus Status);

        private async Task<IntentReconciliationResult> ReconcilePaymentIntentsAsync(
            Guid tenantId,
            DateTime from,
            DateTime to,
            string? stripeAccountId,
            List<DriftDetail> drifts,
            ReconciliationStatus status,
            CancellationToken ct)
        {
            string? lastCursor = null;
            var hasMore = true;
            var totalDriftsManaged = 0;
            var totalRepaired = 0;

            while (hasMore)
            {
                List<StripePaymentIntentDto> intents =
                    await stripeService.ListPaymentIntentsAsync(from, to, stripeAccountId, lastCursor, ct);
                if (intents.Count == 0)
                {
                    break;
                }

                foreach (StripePaymentIntentDto intent in intents.Where(i => i.Status == "succeeded"))
                {
                    DriftDetail? drift = await AnalyzeIntentAsync(intent, tenantId, ct);
                    if (drift == null)
                    {
                        continue;
                    }

                    totalDriftsManaged++;

                    var isConfirmed = await IsDriftConfirmedAsync(tenantId, intent.Id, ct);
                    if (isConfirmed)
                    {
                        var autoRepaired = false;
                        if (drift.Type == DriftType.MissingPayment)
                        {
                            autoRepaired = await AutoRepairDriftAsync(intent, tenantId, ct);
                            if (autoRepaired)
                            {
                                totalRepaired++;
                                metrics.ReconciliationAutoRepairedTotal.Add(1,
                                    new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
                                drift = drift with
                                {
                                    Type = DriftType.None,
                                    Message = drift.Message + " [AUTO-REPAIRED]"
                                };
                                logger.LogInformation("Drift {Id} auto-repaired after confirmation.", intent.Id);
                            }
                        }

                        if (!autoRepaired)
                        {
                            drift = drift with { Message = drift.Message + " [ConfirmedDrift]" };
                            status = ReconciliationStatus.ConfirmedDrift;
                        }
                    }

                    if (drift.Type != DriftType.None)
                    {
                        drifts.Add(drift);
                        metrics.ReconciliationDriftTotal.Add(1,
                            new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()),
                            new KeyValuePair<string, object?>("type", drift.Type.ToString()));
                    }
                }

                lastCursor = intents.Last().Id;
                hasMore = intents.Count == 100;
            }

            return new IntentReconciliationResult(lastCursor, totalDriftsManaged, totalRepaired, status);
        }

        private static (ReconciliationStatus status, ReconciliationSeverity severity) ResolveStatusAndSeverity(
            List<DriftDetail> drifts,
            int totalDriftsManaged,
            int totalRepaired,
            ReconciliationStatus currentStatus)
        {
            ReconciliationStatus status = currentStatus;
            ReconciliationSeverity severity = ReconciliationSeverity.Info;

            if (drifts.Count == 0)
            {
                if (totalRepaired > 0 && totalRepaired == totalDriftsManaged)
                {
                    status = ReconciliationStatus.Repaired;
                }
                else if (totalRepaired > 0)
                {
                    (status, severity) = (ReconciliationStatus.SoftDrift, ReconciliationSeverity.Warning);
                }

                return (status, severity);
            }

            if (status != ReconciliationStatus.ConfirmedDrift)
            {
                status = ReconciliationStatus.SoftDrift;
            }

            if (drifts.Any(static d => d.Severity == ReconciliationSeverity.Critical))
            {
                severity = ReconciliationSeverity.Critical;
                if (status != ReconciliationStatus.ConfirmedDrift) status = ReconciliationStatus.HardDrift;
            }
            else if (drifts.Any(static d => d.Severity == ReconciliationSeverity.Error))
            {
                severity = ReconciliationSeverity.Error;
                if (status != ReconciliationStatus.ConfirmedDrift) status = ReconciliationStatus.HardDrift;
            }
            else if (drifts.Any(static d => d.Severity == ReconciliationSeverity.Warning))
            {
                severity = ReconciliationSeverity.Warning;
                if (status != ReconciliationStatus.ConfirmedDrift) status = ReconciliationStatus.SoftDrift;
            }

            return (status, severity);
        }

        private async Task<decimal> GetLedgerBalanceAsync(Guid tenantId, CancellationToken ct)
        {
            LedgerAccount? cashAccount = await dbContext.LedgerAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code == "1010", ct);

            return cashAccount == null
                ? 0
                : await dbContext.LedgerEntries
                    .AsNoTracking()
                    .Where(e => e.AccountId == cashAccount.Id)
                    .SumAsync(e => e.Debit - e.Credit, ct);
        }

        private async Task<DriftDetail?> AnalyzeIntentAsync(StripePaymentIntentDto intent, Guid tenantId,
            CancellationToken ct)
        {
            var referencePay = $"PAY-STRIPE-{intent.Id}";
            var referenceRec = $"REC-STRIPE-{intent.Id}";

            var ledgerExists = await dbContext.LedgerTransactions
                .AsNoTracking()
                .AnyAsync(
                    t => t.TenantId == tenantId && (t.ReferenceId == referencePay || t.ReferenceId == referenceRec),
                    ct);

            if (ledgerExists)
            {
                return null;
            }

            TimeSpan age = DateTime.UtcNow - intent.Created;
            return age < _timingTolerance
                ? new DriftDetail(intent.Id, DriftType.TimingLag, ReconciliationSeverity.Info,
                    $"Payment succeeded {Math.Round(age.TotalMinutes, 1)}m ago; webhook may be in transit.")
                : new DriftDetail(intent.Id, DriftType.MissingPayment, ReconciliationSeverity.Error,
                    $"Payment ({intent.Amount / 100m} {intent.Currency.ToUpper(System.Globalization.CultureInfo.CurrentCulture)}) succeeded in Stripe but is missing from Ledger after 15m.");
        }

        private async Task<List<DriftDetail>> ReconcileSettlementAsync(
            Guid tenantId,
            DateTime from,
            DateTime to,
            string? stripeAccountId,
            CancellationToken ct)
        {
            var settlementDrifts = new List<DriftDetail>();
            string? lastCursor = null;
            var hasMore = true;

            while (hasMore)
            {
                List<StripeBalanceTransactionDto> transactions =
                    await stripeService.ListBalanceTransactionsAsync(from, to, stripeAccountId, lastCursor, ct);
                if (transactions.Count == 0)
                {
                    break;
                }

                foreach (StripeBalanceTransactionDto tx in transactions)
                {
                    DriftDetail? drift = await AnalyzeBalanceTransactionAsync(tx, tenantId, ct);
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

        private async Task<DriftDetail?> AnalyzeBalanceTransactionAsync(StripeBalanceTransactionDto tx, Guid tenantId,
            CancellationToken ct)
        {
            var referenceId = tx.Type switch
            {
                "payout" => $"PAYOUT-STRIPE-{tx.SourceId ?? tx.Id}",
                "refund" => $"REF-STRIPE-{tx.SourceId ?? tx.Id}",
                "stripe_fee" => $"FEE-STRIPE-{tx.Id}",
                _ => null
            };

            if (referenceId == null)
            {
                return null;
            }

            LedgerTransaction? ledgerTx = await dbContext.LedgerTransactions
                .AsNoTracking()
                .Include(t => t.Entries)
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.ReferenceId == referenceId, ct);

            if (ledgerTx == null)
            {
                TimeSpan age = DateTime.UtcNow - tx.Created;
                return age < _timingTolerance
                    ? new DriftDetail(tx.Id, DriftType.TimingLag, ReconciliationSeverity.Info,
                        $"Settlement {tx.Type} recently created; webhook may be in transit.")
                    : new DriftDetail(tx.Id, DriftType.MissingPayment, ReconciliationSeverity.Error,
                        $"Settlement {tx.Type} ({tx.Amount / 100m} {tx.Currency.ToUpper(System.Globalization.CultureInfo.CurrentCulture)}) missing from Ledger.");
            }

            var ledgerAmount = ledgerTx.Entries.Sum(e => e.Debit - e.Credit);
            var stripeNet = tx.Net / 100m;

            return Math.Abs(Math.Abs(stripeNet) - Math.Abs(ledgerAmount)) > 0.01m
                ? new DriftDetail(tx.Id, DriftType.AmountMismatch, ReconciliationSeverity.Critical,
                    $"Settlement Amount Mismatch: Stripe Net={stripeNet}, Ledger={ledgerAmount}")
                : null;
        }

        private async Task<bool> AutoRepairDriftAsync(StripePaymentIntentDto intent, Guid tenantId,
            CancellationToken ct)
        {
            try
            {
                if (!intent.Metadata.TryGetValue("LoanId", out var loanIdStr) ||
                    !Guid.TryParse(loanIdStr, out var loanId))
                {
                    return false;
                }

                Loan? loan = await dbContext.Loans
                    .FirstOrDefaultAsync(l => l.Id == loanId && l.TenantId == tenantId, ct);

                if (loan == null)
                {
                    return false;
                }

                logger.LogInformation("Auto-repair: re-posting missing payment {IntentId} for loan {LoanId}", intent.Id,
                    loanId);

                var amount = intent.Amount / 100m;
                _ = await postingEngine.PostLoanPaymentAsync(loan, amount, $"STRIPE-{intent.Id}", null, ct);
                _ = await dbContext.SaveChangesAsync(ct);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Auto-repair failed for intent {IntentId}", intent.Id);
                return false;
            }
        }

        private async Task<bool> IsDriftConfirmedAsync(Guid tenantId, string externalId, CancellationToken ct)
        {
            return await dbContext.ReconciliationAudits
                .AsNoTracking()
                .Where(a => a.TenantId == tenantId && a.DriftDetailsJson != null)
                .AnyAsync(a => a.DriftDetailsJson!.Contains(externalId), ct);
        }

        private record DriftDetail(
            string ExternalId,
            DriftType Type,
            ReconciliationSeverity Severity,
            string Message);
    }
}