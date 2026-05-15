using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Idempotency;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Observability.Queries.GetIdempotencyIntrospection
{
    public record GetIdempotencyIntrospectionQuery(string Key) : IRequest<Result<IdempotencyIntrospectionDto>>;

    public class GetIdempotencyIntrospectionHandler(
        IIdempotencyStore idempotencyStore,
        ITenantProvider tenantProvider,
        ILogger<GetIdempotencyIntrospectionHandler> logger)
        : IRequestHandler<GetIdempotencyIntrospectionQuery, Result<IdempotencyIntrospectionDto>>
    {
        public async Task<Result<IdempotencyIntrospectionDto>> Handle(
            GetIdempotencyIntrospectionQuery request,
            CancellationToken cancellationToken)
        {
            Guid? tenantId = tenantProvider.GetTenantId();
            if (tenantId == null)
            {
                logger.LogWarning(
                    "Unauthorized attempt to introspect idempotency key {Key} without valid tenant context.",
                    request.Key);
                return Result.Failure<IdempotencyIntrospectionDto>(DomainErrorCode.Common.UnauthorizedContext);
            }

            logger.LogInformation("Introspecting Idempotency Key {Key} for Tenant {TenantId}", request.Key, tenantId);

            IdempotencyRecord? record =
                await idempotencyStore.GetIdempotencyRecordAsync(tenantId.Value, request.Key, cancellationToken);

            if (record == null)
            {
                return Result.Failure<IdempotencyIntrospectionDto>(DomainErrorCode.Common.EntityNotFound);
            }

            object? parsedBody = null;
            if (!string.IsNullOrWhiteSpace(record.ResponseBody))
            {
                try
                {
                    parsedBody = JsonSerializer.Deserialize<JsonElement>(record.ResponseBody);
                }
                catch (JsonException)
                {
                    parsedBody = record.ResponseBody;
                }
            }

            var dto = new IdempotencyIntrospectionDto(
                record.IdempotencyKey,
                record.Status,
                record.RequestHash,
                record.StatusCode == 0 ? null : record.StatusCode,
                parsedBody,
                record.ResourceType,
                record.ResourceId,
                record.Environment,
                record.CorrelationId,
                record.CausationId,
                record.CreatedAt,
                record.ExpiresAt);

            return Result.Success(dto);
        }
    }
}
