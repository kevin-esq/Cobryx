using Cobryx.Application.Webhooks.Entities;
using Cobryx.Application.Webhooks.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Webhooks.Queries.GetWebhookLogs;

public record WebhookLogDto(
    Guid Id,
    string Provider,
    string ExternalEventId,
    string Status,
    int Attempts,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    string? Error);

public record GetWebhookLogsQuery(
    WebhookStatus? Status = null,
    int Limit = 50,
    int Offset = 0) : IRequest<Result<IEnumerable<WebhookLogDto>>>;

public class GetWebhookLogsHandler(IWebhookEventRepository repository)
    : IRequestHandler<GetWebhookLogsQuery, Result<IEnumerable<WebhookLogDto>>>
{
    public async Task<Result<IEnumerable<WebhookLogDto>>> Handle(GetWebhookLogsQuery request, CancellationToken cancellationToken)
    {
        // Enforce a sensible performance window (last 30 days) for diagnostic logs
        DateTime since = DateTime.UtcNow.AddDays(-30);

        IEnumerable<WebhookEvent> events = await repository.GetRecentAsync(
            request.Status,
            request.Limit,
            request.Offset,
            since,
            cancellationToken);

        IEnumerable<WebhookLogDto> dtos = events.Select(e => new WebhookLogDto(
            e.Id,
            e.Provider,
            e.ExternalEventId,
            e.Status.ToString().ToUpperInvariant(),
            e.Attempts,
            e.CreatedAt,
            e.ProcessedAt,
            e.Error));

        return Result.Success(dtos);
    }
}
