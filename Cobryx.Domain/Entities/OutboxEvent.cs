using System;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

/// <summary>
/// Domain entity representing an event to be processed out-of-process via the Outbox pattern.
/// </summary>
public class OutboxEvent : BaseEntity
{
    public string Type { get; private set; }
    public string Content { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }

    private OutboxEvent()
    {
        Type = null!;
        Content = null!;
    }

    public OutboxEvent(string type, string content)
    {
        Type = type;
        Content = content;
        OccurredOnUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Internal constructor for integration testing to simulate historical events.
    /// </summary>
    internal OutboxEvent(string type, string content, DateTime occurredOnUtc)
    {
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
    }

    public void MarkAsProcessed()
    {
        ProcessedOnUtc = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        UpdateTimestamp();
    }
}
