using Cobryx.Domain.Idempotency;

namespace Cobryx.Application.Observability.Queries.GetIdempotencyIntrospection;

public record IdempotencyIntrospectionDto(
    string IdempotencyKey,
    IdempotencyStatus Status,
    string RequestHash,
    int? StatusCode,
    object? ResponseBody,
    string? ResourceType,
    Guid? ResourceId,
    string? Environment,
    Guid? CorrelationId,
    Guid? CausationId,
    DateTime CreatedAt,
    DateTime ExpiresAt);
