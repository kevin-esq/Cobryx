using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Messaging;
using Cobryx.Domain.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services
{
    public partial class FinancialPostingEngine(ICobryxDbContext context, ILogger<FinancialPostingEngine> logger)
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public async Task<Guid> PostLoanPaymentAllocationAsync(
            Loan loan,
            LoanPaymentAllocation allocation,
            string reference,
            CancellationToken ct = default)
        {
            LogPostingPaymentAllocation(logger, loan.Id, allocation.PaymentId);

            TenantAccounts accounts = await GetTenantSystemAccountsAsync(loan.TenantId, ct);

            var total = allocation.PrincipalApplied + allocation.InterestApplied + allocation.FeesApplied;

            LedgerTransaction tx = CreateTransaction(
                loan.TenantId,
                $"Loan Payment - {reference}",
                $"PAY-{reference}",
                loan.Id);

            tx.AddEntry(accounts.CashAccountId, total, 0);

            AddIfPositive(tx, accounts.PrincipalAccountId, allocation.PrincipalApplied);
            AddIfPositive(tx, accounts.InterestAccountId, allocation.InterestApplied);
            AddIfPositive(tx, accounts.FeeAccountId, allocation.FeesApplied);

            await PersistTransactionAsync(tx, ct);

            var payload = JsonSerializer.Serialize(new
            {
                allocation.PaymentId,
                allocation.LoanId,
                TotalApplied = total,
                Principal = allocation.PrincipalApplied,
                Interest = allocation.InterestApplied,
                Fees = allocation.FeesApplied
            }, _jsonOptions);

            await SaveWithSemanticOutboxAsync(tx, FinancialEventType.PaymentPosted, loan.Id, payload, ct);

            return tx.Id;
        }

        public async Task<Guid> PostLoanPaymentAsync(
            Loan loan,
            decimal amount,
            string reference,
            decimal? platformFee = null,
            CancellationToken ct = default)
        {
            LogPostingPayment(logger, amount, loan.Id);

            TenantAccounts accounts = await GetTenantSystemAccountsAsync(loan.TenantId, ct);
            PaymentSplit split = CalculatePaymentSplit(loan, amount);

            LedgerTransaction tx = CreateTransaction(
                loan.TenantId,
                $"Loan Payment - {reference}",
                $"PAY-{reference}",
                loan.Id);

            tx.AddEntry(accounts.CashAccountId, amount, 0);

            AddIfPositive(tx, accounts.PrincipalAccountId, split.PrincipalAmount);
            AddIfPositive(tx, accounts.InterestAccountId, split.InterestAmount);
            AddIfPositive(tx, accounts.FeeAccountId, split.FeeAmount);

            await PersistTransactionAsync(tx, ct);

            if (platformFee is > 0)
            {
                TenantAccounts platformAccounts =
                    await GetTenantSystemAccountsAsync(CobryxDefaults.PlatformTenantId, ct);

                LedgerTransaction platformTx = CreateTransaction(
                    CobryxDefaults.PlatformTenantId,
                    $"Platform Fee - {reference} (Tenant: {loan.TenantId})",
                    $"FEE-{reference}");

                platformTx.AddEntry(platformAccounts.CashAccountId, platformFee.Value, 0);
                platformTx.AddEntry(platformAccounts.FeeAccountId, 0, platformFee.Value);

                await PersistTransactionAsync(platformTx, ct);

                var platformPayload = JsonSerializer.Serialize(new { PlatformFee = platformFee }, _jsonOptions);

                await SaveWithSemanticOutboxAsync(
                    platformTx,
                    FinancialEventType.LateFeeApplied,
                    loan.Id,
                    platformPayload,
                    ct);
            }

            var payload = JsonSerializer.Serialize(split, _jsonOptions);

            await SaveWithSemanticOutboxAsync(
                tx,
                FinancialEventType.PaymentPosted,
                loan.Id,
                payload,
                ct);

            return tx.Id;
        }

        public async Task<Guid> PostReversalAsync(
            Guid originalTransactionId,
            decimal amount,
            string reason,
            CancellationToken ct = default)
        {
            LedgerTransaction original = await context.LedgerTransactions
                                             .Include(static t => t.Entries)
                                             .FirstOrDefaultAsync(t => t.Id == originalTransactionId, ct)
                                         ?? throw new DomainException(DomainErrorCode.Common.GeneralError);

            List<LedgerTransaction> existingReversals = await context.LedgerTransactions
                .Where(t => t.OriginalTransactionId == originalTransactionId && t.IsPosted)
                .Include(static t => t.Entries)
                .ToListAsync(ct);

            var alreadyReversed = existingReversals
                .SelectMany(static t => t.Entries)
                .Sum(static e => Math.Abs(e.Debit));

            var originalTotal = original.Entries.Sum(static e => Math.Abs(e.Debit));
            var remaining = Math.Max(0, originalTotal - alreadyReversed);

            if (amount > remaining + 0.01m)
            {
                throw new DomainException(DomainErrorCode.Common.GeneralError);
            }

            var reversal = LedgerTransaction.CreatePartialReversal(original, amount, $"REVERSAL: {reason}");
            _ = context.LedgerTransactions.Add(reversal);

            if (TryHandlePlatformReversal(original, originalTotal, amount, reason, ct) is { } platformTask)
            {
                _ = await platformTask;
            }

            var payload = JsonSerializer.Serialize(new
            {
                OriginalTransactionId = originalTransactionId,
                ReversalAmount = amount,
                Reason = reason
            }, _jsonOptions);

            await SaveWithSemanticOutboxAsync(
                reversal,
                FinancialEventType.ReversalPosted,
                original.LoanId ?? Guid.Empty,
                payload,
                ct);

            return reversal.Id;
        }

        public async Task<Guid> PostChargeOffAsync(Loan loan, string reason, CancellationToken ct = default)
        {
            TenantAccounts accounts = await GetTenantSystemAccountsAsync(loan.TenantId, ct);

            var total = loan.CurrentPrincipalBalance +
                        loan.CurrentInterestBalance +
                        loan.CurrentLateFeeBalance;

            if (total <= 0)
            {
                return Guid.Empty;
            }

            LedgerTransaction tx = CreateTransaction(
                loan.TenantId,
                $"CHARGE-OFF ({loan.LoanNumber}): {reason}",
                $"CHG-{loan.Id}",
                loan.Id);

            tx.AddEntry(accounts.LossExpenseId, total, 0);
            tx.AddEntry(accounts.PrincipalAccountId, 0, loan.CurrentPrincipalBalance);

            AddIfPositive(tx, accounts.InterestAccountId, loan.CurrentInterestBalance);
            AddIfPositive(tx, accounts.FeeAccountId, loan.CurrentLateFeeBalance);

            await PersistTransactionAsync(tx, ct);

            var payload = JsonSerializer.Serialize(new { TotalOutstanding = total, Reason = reason }, _jsonOptions);

            await SaveWithSemanticOutboxAsync(
                tx,
                FinancialEventType.LoanWriteOff,
                loan.Id,
                payload,
                ct);

            return tx.Id;
        }

        public async Task<Guid> PostRecoveryAsync(
            Loan loan,
            decimal amount,
            string reference,
            CancellationToken ct = default)
        {
            TenantAccounts accounts = await GetTenantSystemAccountsAsync(loan.TenantId, ct);

            LedgerTransaction tx = CreateTransaction(
                loan.TenantId,
                $"RECOVERY: {reference}",
                $"REC-{reference}",
                loan.Id);

            tx.AddEntry(accounts.CashAccountId, amount, 0);
            tx.AddEntry(accounts.RecoveryIncomeId, 0, amount);

            await PersistTransactionAsync(tx, ct);

            var payload = JsonSerializer.Serialize(new { Amount = amount, Reference = reference }, _jsonOptions);

            await SaveWithSemanticOutboxAsync(
                tx,
                FinancialEventType.RecoveryPayment,
                loan.Id,
                payload,
                ct);

            return tx.Id;
        }

        private async Task PersistTransactionAsync(LedgerTransaction transaction, CancellationToken ct)
        {
            transaction.Post();
            _ = context.LedgerTransactions.Add(transaction);
            _ = await context.SaveChangesAsync(ct);
        }

        private static LedgerTransaction CreateTransaction(
            Guid tenantId,
            string description,
            string reference,
            Guid? loanId = null)
            => new(tenantId, description, reference, loanId);

        private static void AddIfPositive(LedgerTransaction tx, Guid accountId, decimal amount)
        {
            if (amount > 0)
            {
                tx.AddEntry(accountId, 0, amount);
            }
        }

        private async Task<Task?> TryHandlePlatformReversal(
            LedgerTransaction original,
            decimal originalTotal,
            decimal amount,
            string reason,
            CancellationToken ct)
        {
            if (original.ReferenceId?.StartsWith("PAY-", StringComparison.Ordinal) != true)
            {
                return null;
            }

            var feeRef = original.ReferenceId.Replace("PAY-", "FEE-", StringComparison.Ordinal);

            LedgerTransaction? platformTx = await context.LedgerTransactions
                .Include(static t => t.Entries)
                .FirstOrDefaultAsync(t =>
                    t.ReferenceId == feeRef &&
                    t.TenantId == CobryxDefaults.PlatformTenantId, ct);

            if (platformTx is null)
            {
                return null;
            }

            var platformTotal = platformTx.Entries.Sum(static e => Math.Abs(e.Debit));
            var ratio = amount / originalTotal;
            var refund = Math.Round(platformTotal * ratio, 2);

            var reversal = LedgerTransaction.CreatePartialReversal(platformTx, refund, $"REVERSAL (FEE): {reason}");
            _ = context.LedgerTransactions.Add(reversal);

            var payload = JsonSerializer.Serialize(new
            {
                OriginalTransactionId = original.Id,
                ReversalAmount = refund,
                Reason = reason
            }, _jsonOptions);

            return SaveWithSemanticOutboxAsync(
                reversal,
                FinancialEventType.ReversalPosted,
                original.LoanId ?? Guid.Empty,
                payload,
                ct);
        }

        /// <summary>
        /// ATOMIC: Saves ledger transaction AND outbox message in SINGLE transaction.
        /// This guarantees that if the ledger commits, the outbox event is also committed.
        /// The outbox worker will then process the side effect with retry + DLQ.
        /// </summary>
        private async Task SaveWithSemanticOutboxAsync(
            LedgerTransaction transaction,
            FinancialEventType eventType,
            Guid entityId,
            string payloadJson,
            CancellationToken ct)
        {
            // Get sequence ID from entries (already tracked)
            var sequenceId = transaction.Entries
                .OrderBy(static e => e.JournalSequenceId)
                .FirstOrDefault()?.JournalSequenceId ?? 0;

            // Create outbox message BEFORE SaveChanges
            var message = new OutboxMessage(
                transaction.TenantId,
                eventType.ToString(),
                payloadJson,
                entityId,
                sequenceId,
                transaction.TenantId.ToString());

            _ = context.OutboxMessages.Add(message);

            // SINGLE SaveChanges = ATOMIC commit of both ledger + outbox
            _ = await context.SaveChangesAsync(ct);
        }

        private async Task<TenantAccounts> GetTenantSystemAccountsAsync(Guid tenantId, CancellationToken ct)
        {
            List<LedgerAccount> accounts = await context.LedgerAccounts
                .Where(a => a.TenantId == tenantId && a.IsSystem)
                .ToListAsync(ct);

            return new TenantAccounts(
                accounts.First(static a => a.Code == "1010").Id,
                accounts.First(static a => a.Code == "1210").Id,
                accounts.First(static a => a.Code == "4010").Id,
                accounts.First(static a => a.Code == "4020").Id,
                accounts.First(static a => a.Code == "5010").Id,
                accounts.First(static a => a.Code == "4030").Id
            );
        }

        private static PaymentSplit CalculatePaymentSplit(Loan loan, decimal amount)
        {
            var remaining = amount;

            var fee = Math.Min(remaining, loan.CurrentLateFeeBalance);
            remaining -= fee;

            var interest = Math.Min(remaining, loan.CurrentInterestBalance);
            remaining -= interest;

            var principal = Math.Min(remaining, loan.CurrentPrincipalBalance);

            return new PaymentSplit(principal, interest, fee);
        }

        public sealed record TenantAccounts(
            Guid CashAccountId,
            Guid PrincipalAccountId,
            Guid InterestAccountId,
            Guid FeeAccountId,
            Guid LossExpenseId,
            Guid RecoveryIncomeId);

        public sealed record PaymentSplit(
            decimal PrincipalAmount,
            decimal InterestAmount,
            decimal FeeAmount);
    }
}
