using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Lending;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cobryx.Application.Accounting.Services
{
    public partial class ReconciliationEngine(
        ICobryxDbContext dbContext,
        IStripeService stripeService,
        FinancialPostingEngine postingEngine,
        CobryxMetrics metrics,
        IClock clock,
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
            LogReconciliationStarted(logger, runId, tenantId);

            var (available, pending) = await stripeService.GetBalanceAsync(stripeAccountId, ct);
            var ledgerBalance = await GetLedgerBalanceAsync(tenantId, ct);

            List<DriftDetail> drifts = [];
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

            ReconciliationAudit audit = new(
                tenantId, runId, from, to,
                ledgerBalance, available, pending,
                status, severity,
                drifts.Count,
                JsonConvert.SerializeObject(drifts, new StringEnumConverter()),
                null,
                lastCursor);

            _ = dbContext.ReconciliationAudits.Add(audit);
            _ = await dbContext.SaveChangesAsync(ct);

            LogReconciliationCompleted(logger, runId, audit.Status);
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

                foreach (StripePaymentIntentDto intent in intents.Where(static i => i.Status == StripeConstants.PaymentIntentStatuses.Succeeded))
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
                                LogDriftAutoRepaired(logger, intent.Id);
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
                if (status != ReconciliationStatus.ConfirmedDrift)
                {
                    status = ReconciliationStatus.HardDrift;
                }
            }
            else if (drifts.Any(static d => d.Severity == ReconciliationSeverity.Error))
            {
                severity = ReconciliationSeverity.Error;
                if (status != ReconciliationStatus.ConfirmedDrift)
                {
                    status = ReconciliationStatus.HardDrift;
                }
            }
            else if (drifts.Any(static d => d.Severity == ReconciliationSeverity.Warning))
            {
                severity = ReconciliationSeverity.Warning;
                if (status != ReconciliationStatus.ConfirmedDrift)
                {
                    status = ReconciliationStatus.SoftDrift;
                }
            }

            return (status, severity);
        }

        private async Task<decimal> GetLedgerBalanceAsync(Guid tenantId, CancellationToken ct)
        {
            LedgerAccount? cashAccount = await dbContext.LedgerAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Code == "1010", ct);

            return cashAccount == null
                ? 0
                : await dbContext.LedgerEntries
                    .AsNoTracking()
                    .Where(e => e.AccountId == cashAccount.Id)
                    .SumAsync(static e => e.Debit - e.Credit, ct);
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

            // BIDIRECTIONAL CHECK: Stripe → DB
            if (!ledgerExists)
            {
                TimeSpan age = clock.UtcNow - intent.Created;
                return age < _timingTolerance
                    ? new DriftDetail(intent.Id, DriftType.TimingLag, ReconciliationSeverity.Info,
                        $"Payment succeeded {Math.Round(age.TotalMinutes, 1)}m ago; webhook may be in transit.")
                    : new DriftDetail(intent.Id, DriftType.MissingPayment, ReconciliationSeverity.Error,
                        $"Payment ({intent.Amount / 100m} {intent.Currency.ToUpper(System.Globalization.CultureInfo.CurrentCulture)}) succeeded in Stripe but is missing from Ledger after 15m.");
            }

            // BIDIRECTIONAL CHECK: DB → Stripe (verify amounts match)
            // Query entries separately for amount verification
            var ledgerAmount = await dbContext.LedgerEntries
                .AsNoTracking()
                .Where(e => dbContext.LedgerTransactions
                    .Any(t => t.Id == e.TransactionId &&
                              t.TenantId == tenantId &&
                              (t.ReferenceId == referencePay || t.ReferenceId == referenceRec)))
                .SumAsync(static e => e.Debit - e.Credit, ct);

            var stripeAmount = intent.Amount / 100m;

            return Math.Abs(Math.Abs(ledgerAmount) - stripeAmount) > 0.01m && ledgerAmount != 0
                ? new DriftDetail(intent.Id, DriftType.AmountMismatch, ReconciliationSeverity.Critical,
                    $"CRITICAL: Amount mismatch! Ledger={Math.Abs(ledgerAmount):C}, Stripe={stripeAmount:C}")
                : null;
        }

        private async Task<List<DriftDetail>> ReconcileSettlementAsync(
            Guid tenantId,
            DateTime from,
            DateTime to,
            string? stripeAccountId,
            CancellationToken ct)
        {
            List<DriftDetail> settlementDrifts = [];
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
                .Include(static t => t.Entries)
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.ReferenceId == referenceId, ct);

            if (ledgerTx == null)
            {
                TimeSpan age = clock.UtcNow - tx.Created;
                return age < _timingTolerance
                    ? new DriftDetail(tx.Id, DriftType.TimingLag, ReconciliationSeverity.Info,
                        $"Settlement {tx.Type} recently created; webhook may be in transit.")
                    : new DriftDetail(tx.Id, DriftType.MissingPayment, ReconciliationSeverity.Error,
                        $"Settlement {tx.Type} ({tx.Amount / 100m} {tx.Currency.ToUpper(System.Globalization.CultureInfo.CurrentCulture)}) missing from Ledger.");
            }

            var ledgerAmount = ledgerTx.Entries.Sum(static e => e.Debit - e.Credit);
            var stripeNet = tx.Net / 100m;

            return Math.Abs(Math.Abs(stripeNet) - Math.Abs(ledgerAmount)) > 0.01m
                ? new DriftDetail(tx.Id, DriftType.AmountMismatch, ReconciliationSeverity.Critical,
                    $"Settlement Amount Mismatch: Stripe Net={stripeNet}, Ledger={ledgerAmount}")
                : null;
        }

        /// <summary>
        /// ATOMIC auto-repair: Payment + Ledger entries in single transaction.
        /// If any part fails, entire operation rolls back - no partial state.
        /// </summary>
        private async Task<bool> AutoRepairDriftAsync(StripePaymentIntentDto intent, Guid tenantId,
            CancellationToken ct)
        {
            if (!intent.Metadata.TryGetValue("LoanId", out var loanIdStr) ||
                !Guid.TryParse(loanIdStr, out Guid loanId))
            {
                return false;
            }

            Loan? loan = await dbContext.Loans
                .FirstOrDefaultAsync(l => l.Id == loanId && l.TenantId == tenantId, ct);

            if (loan == null)
            {
                return false;
            }

            // ATOMIC REPAIR: Use explicit transaction for all-or-nothing semantics
            // Note: InMemoryDatabase doesn't support transactions, so we handle that gracefully
            var supportsTransactions = !dbContext.Database.ProviderName?.Contains("InMemory") ?? true;

            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            try
            {
                if (supportsTransactions)
                {
                    transaction = await dbContext.Database.BeginTransactionAsync(ct);
                }

                LogAutoRepairPosting(logger, intent.Id, loanId);

                var amount = intent.Amount / 100m;

                // 1. Create payment record (if applicable)
                // 2. Post to ledger
                // Both happen within same transaction
                _ = await postingEngine.PostLoanPaymentAsync(loan, amount, $"STRIPE-{intent.Id}", null, ct);

                // Save all changes
                _ = await dbContext.SaveChangesAsync(ct);

                // Commit transaction - atomic success
                if (transaction != null)
                {
                    await transaction.CommitAsync(ct);
                }

                LogAutoRepairSuccess(logger, intent.Id, loanId);
                return true;
            }
            catch (Exception ex)
            {
                // Rollback on any failure - no partial state
                if (transaction != null)
                {
                    await transaction.RollbackAsync(ct);
                }

                LogAutoRepairFailed(logger, ex, intent.Id);
                metrics.AutoRepairAtomicFailureTotal.Add(1,
                    new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()),
                    new KeyValuePair<string, object?>("intent_id", intent.Id));

                return false;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        private async Task<bool> IsDriftConfirmedAsync(Guid tenantId, string externalId, CancellationToken ct)
        {
            return await dbContext.ReconciliationAudits
                .AsNoTracking()
                .Where(a => a.TenantId == tenantId && a.DriftDetailsJson != null)
                .AnyAsync(a => a.DriftDetailsJson!.Contains(externalId), ct);
        }

        // ReSharper disable once NotAccessedPositionalProperty.Local — Used for record identity, JSON serialization, and equality
        private record DriftDetail(
            string ExternalId,
            DriftType Type,
            ReconciliationSeverity Severity,
            string Message);
    }
}
