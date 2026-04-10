using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Messaging;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Messaging
{
    /// <summary>
    /// A semantic event consumer that maintains a parallel shadow balance.
    /// Used to verify Phase 12 stability through balance parity verification.
    /// </summary>
    public class EventShadowReplayEngine(
        ICobryxDbContext context,
        CobryxMetrics metrics) : IFinancialEventConsumer
    {
        public string Name => "EventShadowReplayEngine";

        public async Task ConsumeAsync(OutboxMessage @event, CancellationToken ct = default)
        {
            if (@event.LedgerSequenceId == null)
            {
                return;
            }

            var alreadyProcessed = await context.ProcessedEvents
                .AnyAsync(p => p.EventId == @event.Id && p.ConsumerName == Name, ct);

            if (alreadyProcessed)
            {
                return;
            }

            if (@event.Type == nameof(FinancialEventType.PaymentPosted))
            {
                await HandlePaymentAsync(@event, ct);
            }
            else if (@event.Type == nameof(FinancialEventType.LoanWriteOff))
            {
                await HandleWriteOffAsync(@event, ct);
            }

            _ = context.ProcessedEvents.Add(new ProcessedEvent(@event.Id, Name));
            _ = await context.SaveChangesAsync(ct);

            metrics.ShadowReplayEventsProcessed.Add(1);
        }

        private async Task HandlePaymentAsync(OutboxMessage @event, CancellationToken ct)
        {
            PaymentSplitPayload? split = JsonSerializer.Deserialize<PaymentSplitPayload>(@event.Payload);
            if (split == null)
            {
                return;
            }

            TenantAccounts accounts = await GetTenantSystemAccountsAsync(@event.TenantId, ct);

            await ApplyShadowChangeAsync(@event.TenantId, accounts.CashAccountId,
                split.PrincipalAmount + split.InterestAmount + split.FeeAmount, @event.LedgerSequenceId!.Value, ct);

            if (split.PrincipalAmount > 0)
            {
                await ApplyShadowChangeAsync(@event.TenantId, accounts.PrincipalAccountId, -split.PrincipalAmount,
                    @event.LedgerSequenceId!.Value, ct);
            }

            if (split.InterestAmount > 0)
            {
                await ApplyShadowChangeAsync(@event.TenantId, accounts.InterestAccountId, split.InterestAmount,
                    @event.LedgerSequenceId!.Value, ct);
            }

            if (split.FeeAmount > 0)
            {
                await ApplyShadowChangeAsync(@event.TenantId, accounts.FeeAccountId, split.FeeAmount,
                    @event.LedgerSequenceId!.Value, ct);
            }
        }

        private async Task HandleWriteOffAsync(OutboxMessage @event, CancellationToken ct)
        {
            WriteOffPayload? payload = JsonSerializer.Deserialize<WriteOffPayload>(@event.Payload);
            if (payload == null)
            {
                return;
            }

            TenantAccounts accounts = await GetTenantSystemAccountsAsync(@event.TenantId, ct);

            await ApplyShadowChangeAsync(@event.TenantId, accounts.PrincipalAccountId, -payload.TotalOutstanding,
                @event.LedgerSequenceId!.Value, ct);
            await ApplyShadowChangeAsync(@event.TenantId, accounts.LossExpenseId, payload.TotalOutstanding,
                @event.LedgerSequenceId!.Value, ct);
        }

        private async Task ApplyShadowChangeAsync(Guid tenantId, Guid accountId, decimal delta, long sequence,
            CancellationToken ct)
        {
            EventShadowBalance? shadow = await context.EventShadowBalances
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.AccountId == accountId, ct);

            if (shadow == null)
            {
                shadow = new EventShadowBalance(tenantId, accountId, delta, sequence);
                _ = context.EventShadowBalances.Add(shadow);
            }
            else
            {
                shadow.ApplyChange(delta, sequence);
            }
        }

        private async Task<TenantAccounts> GetTenantSystemAccountsAsync(Guid tenantId, CancellationToken ct)
        {
            List<LedgerAccount> accounts = await context.LedgerAccounts
                .Where(a => a.TenantId == tenantId && a.IsSystem)
                .ToListAsync(ct);

            return new TenantAccounts(
                CashAccountId: accounts.First(a => a.Code == "1010").Id,
                PrincipalAccountId: accounts.First(a => a.Code == "1210").Id,
                InterestAccountId: accounts.First(a => a.Code == "4010").Id,
                FeeAccountId: accounts.First(a => a.Code == "4020").Id,
                LossExpenseId: accounts.First(a => a.Code == "5010").Id
            );
        }

        private record TenantAccounts(
            Guid CashAccountId,
            Guid PrincipalAccountId,
            Guid InterestAccountId,
            Guid FeeAccountId,
            Guid LossExpenseId);

        private record PaymentSplitPayload(decimal PrincipalAmount, decimal InterestAmount, decimal FeeAmount);

        private record WriteOffPayload(decimal TotalOutstanding, string Reason);
    }
}
