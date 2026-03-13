using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Messaging;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Messaging;

/// <summary>
/// A semantic event consumer that maintains a parallel shadow balance.
/// Used to verify Phase 12 stability through balance parity verification.
/// </summary>
public class EventShadowReplayEngine(
    ICobryxDbContext context,
    CobryxMetrics metrics,
    ILogger<EventShadowReplayEngine> logger) : IFinancialEventConsumer
{
    private readonly ICobryxDbContext _context = context;
    private readonly CobryxMetrics _metrics = metrics;
    private readonly ILogger<EventShadowReplayEngine> _logger = logger;

    public string Name => "EventShadowReplayEngine";

    public async Task ConsumeAsync(OutboxMessage @event, CancellationToken ct = default)
    {
        if (@event.LedgerSequenceId == null) return;
        var alreadyProcessed = await _context.ProcessedEvents
            .AnyAsync(p => p.EventId == @event.Id && p.ConsumerName == Name, ct);

        if (alreadyProcessed) return;

        if (@event.Type == FinancialEventType.PaymentPosted.ToString())
        {
            await HandlePaymentAsync(@event, ct);
        }
        else if (@event.Type == FinancialEventType.LoanWriteOff.ToString())
        {
            await HandleWriteOffAsync(@event, ct);
        }

        _context.ProcessedEvents.Add(new ProcessedEvent(@event.Id, Name));
        await _context.SaveChangesAsync(ct);

        _metrics.ShadowReplayEventsProcessed.Add(1);
    }

    private async Task HandlePaymentAsync(OutboxMessage @event, CancellationToken ct)
    {
        var split = JsonSerializer.Deserialize<PaymentSplitPayload>(@event.Payload);
        if (split == null) return;

        var accounts = await GetTenantSystemAccountsAsync(@event.TenantId, ct);

        // Debit Cash (Asset Increases)
        await ApplyShadowChangeAsync(@event.TenantId, accounts.CashAccountId, split.PrincipalAmount + split.InterestAmount + split.FeeAmount, @event.LedgerSequenceId!.Value, ct);

        // Credit Principal (Asset Decreases)
        if (split.PrincipalAmount > 0)
            await ApplyShadowChangeAsync(@event.TenantId, accounts.PrincipalAccountId, -split.PrincipalAmount, @event.LedgerSequenceId!.Value, ct);

        // Credit Interest (Revenue Increases)
        if (split.InterestAmount > 0)
            await ApplyShadowChangeAsync(@event.TenantId, accounts.InterestAccountId, split.InterestAmount, @event.LedgerSequenceId!.Value, ct);

        // Credit Fees (Revenue Increases)
        if (split.FeeAmount > 0)
            await ApplyShadowChangeAsync(@event.TenantId, accounts.FeeAccountId, split.FeeAmount, @event.LedgerSequenceId!.Value, ct);
    }

    private async Task HandleWriteOffAsync(OutboxMessage @event, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<WriteOffPayload>(@event.Payload);
        if (payload == null) return;

        var accounts = await GetTenantSystemAccountsAsync(@event.TenantId, ct);

        // Movement from Asset (Principal) to Expense (Loss)
        await ApplyShadowChangeAsync(@event.TenantId, accounts.PrincipalAccountId, -payload.TotalOutstanding, @event.LedgerSequenceId!.Value, ct);
        await ApplyShadowChangeAsync(@event.TenantId, accounts.LossExpenseId, payload.TotalOutstanding, @event.LedgerSequenceId!.Value, ct);
    }

    private async Task ApplyShadowChangeAsync(Guid tenantId, Guid accountId, decimal delta, long sequence, CancellationToken ct)
    {
        var shadow = await _context.EventShadowBalances
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.AccountId == accountId, ct);

        if (shadow == null)
        {
            shadow = new EventShadowBalance(tenantId, accountId, delta, sequence);
            _context.EventShadowBalances.Add(shadow);
        }
        else
        {
            shadow.ApplyChange(delta, sequence);
        }
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

    private record TenantAccounts(
        Guid CashAccountId,
        Guid PrincipalAccountId,
        Guid InterestAccountId,
        Guid FeeAccountId,
        Guid LossExpenseId,
        Guid RecoveryIncomeId);

    private record PaymentSplitPayload(decimal PrincipalAmount, decimal InterestAmount, decimal FeeAmount);
    private record WriteOffPayload(decimal TotalOutstanding, string Reason);
}
