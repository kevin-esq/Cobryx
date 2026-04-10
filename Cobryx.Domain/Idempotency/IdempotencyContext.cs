namespace Cobryx.Domain.Idempotency;

/// <summary>
/// Context for the current idempotent request.
/// Allows handlers to complete idempotency within their transaction boundary.
/// </summary>
public class IdempotencyContext(Guid tenantId, string idempotencyKey)
{
    public Guid TenantId { get; } = tenantId;
    public string IdempotencyKey { get; } = idempotencyKey;
    public bool IsCompleted { get; private set; }

    public void MarkCompleted() => IsCompleted = true;
}
