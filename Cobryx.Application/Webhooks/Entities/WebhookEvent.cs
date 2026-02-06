using System;
using Cobryx.Domain.Common;

namespace Cobryx.Application.Webhooks.Entities;

public enum WebhookStatus
{
    Pending,
    Processing,
    Processed,
    Failed
}

public class WebhookEvent : BaseEntity
{
    public string Provider { get; private set; }
    public string ExternalEventId { get; private set; }
    public string RawPayload { get; private set; }
    public WebhookStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

    private WebhookEvent()
    {
        Provider = null!;
        ExternalEventId = null!;
        RawPayload = null!;
    }

    public WebhookEvent(string provider, string externalEventId, string rawPayload)
    {
        Provider = provider;
        ExternalEventId = externalEventId;
        RawPayload = rawPayload;
        Status = WebhookStatus.Pending;
        Attempts = 0;
    }

    public void StartProcessing()
    {
        Status = WebhookStatus.Processing;
        Attempts++;
        LastAttemptAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void MarkAsProcessed()
    {
        Status = WebhookStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void MarkAsFailed(string error)
    {
        Status = WebhookStatus.Failed;
        Error = error;
        UpdateTimestamp();
    }

    public void ResetToPending()
    {
        Status = WebhookStatus.Pending;
        UpdateTimestamp();
    }
}
