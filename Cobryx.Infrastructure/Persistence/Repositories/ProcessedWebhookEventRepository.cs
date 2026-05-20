using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Webhooks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for domain-level webhook idempotency with atomic operations.
/// </summary>
public partial class ProcessedWebhookEventRepository(
    CobryxDbContext context,
    ILogger<ProcessedWebhookEventRepository> logger) : IProcessedWebhookEventRepository
{
    public async Task<bool> ExistsAsync(string provider, string eventId, CancellationToken ct = default)
    {
        return await context.ProcessedWebhookEvents
            .AnyAsync(e => e.Provider == provider && e.EventId == eventId, ct);
    }

    public async Task<bool> ExistsByPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        return await context.ProcessedWebhookEvents
            .AnyAsync(e => e.PaymentIntentId == paymentIntentId, ct);
    }

    public async Task AddAsync(ProcessedWebhookEvent processedEvent, CancellationToken ct = default)
    {
        context.ProcessedWebhookEvents.Add(processedEvent);
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> TryMarkAsProcessedAsync(
        string provider,
        string eventId,
        string eventType,
        string? paymentIntentId = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default)
    {
        var processedEvent = new ProcessedWebhookEvent(
            provider, eventId, eventType, paymentIntentId, relatedEntityId, relatedEntityType);

        context.ProcessedWebhookEvents.Add(processedEvent);

        try
        {
            await context.SaveChangesAsync(ct);
            LogEventMarkedAsProcessed(logger, provider, eventId);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Another process already marked this event as processed
            context.Entry(processedEvent).State = EntityState.Detached;
            LogEventAlreadyProcessed(logger, provider, eventId);
            return false;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("23505") ||
               message.Contains("2601") ||
               message.Contains("2627") ||
               message.Contains("UNIQUE constraint") ||
               message.Contains("duplicate key") ||
               message.Contains("ix_processed_webhook_events");
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Webhook event marked as processed: {Provider}/{EventId}")]
    private static partial void LogEventMarkedAsProcessed(ILogger logger, string provider, string eventId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Webhook event already processed (idempotency hit): {Provider}/{EventId}")]
    private static partial void LogEventAlreadyProcessed(ILogger logger, string provider, string eventId);

    public async Task RemoveAsync(string provider, string eventId, CancellationToken ct = default)
    {
        await context.ProcessedWebhookEvents
            .Where(e => e.Provider == provider && e.EventId == eventId)
            .ExecuteDeleteAsync(ct);

        LogEventRemoved(logger, provider, eventId);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook event idempotency record removed (Forced Replay): {Provider}/{EventId}")]
    private static partial void LogEventRemoved(ILogger logger, string provider, string eventId);
}
