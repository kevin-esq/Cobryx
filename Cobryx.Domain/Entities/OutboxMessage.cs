using System;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class OutboxMessage : BaseEntity
{
    public string Type { get; private set; }
    public string Content { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage()
    {
        Type = null!;
        Content = null!;
    }

    public OutboxMessage(string type, string content)
    {
        Type = type;
        Content = content;
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        UpdateTimestamp();
    }
}
