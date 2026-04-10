using Cobryx.Domain.Shared;

namespace Cobryx.Application.Webhooks.Entities
{
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

        // ReSharper disable once UnusedMember.Local — Required by EF Core
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
            RawPayload = string.IsNullOrWhiteSpace(rawPayload) ? "{}" : rawPayload;
            Status = WebhookStatus.Pending;
            Attempts = 0;
        }

        public void StartProcessing(DateTime now)
        {
            Status = WebhookStatus.Processing;
            Attempts++;
            LastAttemptAt = now;
            UpdateTimestamp();
        }

        public void MarkAsProcessed(DateTime now)
        {
            Status = WebhookStatus.Processed;
            ProcessedAt = now;
            UpdateTimestamp();
        }

        public void MarkAsFailed(string error)
        {
            Status = WebhookStatus.Failed;
            Error = error;
            UpdateTimestamp();
        }

        // ReSharper disable once UnusedMember.Global — Public API for webhook retry flows
        public void ResetToPending()
        {
            Status = WebhookStatus.Pending;
            UpdateTimestamp();
        }
    }
}
