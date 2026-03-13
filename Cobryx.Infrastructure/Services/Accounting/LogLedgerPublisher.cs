using System.Text.Json;

using Cobryx.Application.Accounting.Events;
using Cobryx.Application.Common.Interfaces;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Services.Accounting;

public class LogLedgerPublisher(ILogger<LogLedgerPublisher> logger) : ILedgerPublisher
{
    private readonly ILogger<LogLedgerPublisher> _logger = logger;

    public Task PublishAsync(IEnumerable<LedgerCdcEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            LogEvent(@event);
        }
        return Task.CompletedTask;
    }

    public Task PublishAsync(LedgerCdcEvent @event, CancellationToken ct = default)
    {
        LogEvent(@event);
        return Task.CompletedTask;
    }

    private void LogEvent(LedgerCdcEvent @event)
    {
        var json = JsonSerializer.Serialize(@event);
        _logger.LogInformation("[LEDGER-CDC] {JournalSequenceId} | {TenantId} | {Payload}",
            @event.JournalSequenceId, @event.TenantId, json);
    }
}
