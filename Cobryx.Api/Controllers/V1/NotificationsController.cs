using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Notifications.Commands.BulkNotificationAction;
using Cobryx.Application.Notifications.Commands.MarkNotificationRead;
using Cobryx.Application.Notifications.Common;
using Cobryx.Application.Notifications.Queries.GetNotifications;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Manages system alerts and tenant notifications including read-state tracking.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Tags("Platform")]
public class NotificationsController(ISender sender) : CobryxBaseController(sender)
{
    /// <summary>
    /// Retrieves a list of notifications for the current tenant.
    /// </summary>
    /// <param name="unreadOnly">If true, only returns notifications that haven't been marked as read.</param>
    /// <param name="limit">Maximum number of results to return (default 20).</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Returns notifications ordered by creation date (newest first).
    /// Soft-deleted notifications are excluded automatically.
    ///
    /// Possible Outcomes:
    /// - NOTIFICATION.SEARCH_SUCCESS: Notifications retrieved successfully.
    /// </remarks>
    /// <response code="200">A collection of notifications.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<NotificationDto>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool unreadOnly = true,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        Result<List<NotificationDto>> result = await Sender.Send(
            new GetNotificationsQuery(unreadOnly, limit), ct);
        return HandleResult(result, NotificationOutcomes.SearchCompleted);
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    /// <param name="id">Unique identifier of the notification.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Idempotent: marking an already-read notification has no effect.
    ///
    /// Possible Outcomes:
    /// - NOTIFICATION.MARKED_READ: Notification acknowledged.
    /// </remarks>
    /// <response code="200">Notification marked as read.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    /// <response code="404">Notification not found or belongs to another tenant.</response>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        Result result = await Sender.Send(new MarkNotificationReadCommand(id), ct);
        return HandleResult(result, NotificationOutcomes.MarkedAsRead);
    }

    /// <summary>
    /// Performs a bulk action on multiple notifications (e.g., mark as read).
    /// </summary>
    /// <param name="request">List of notification IDs and the operation to perform.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Currently supports: 'mark_read'.
    /// </remarks>
    /// <response code="200">Bulk operation completed.</response>
    /// <response code="400">Invalid operation or malformed list.</response>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> BulkAction([FromBody] BulkNotificationActionRequest request, CancellationToken ct)
    {
        Result result = await Sender.Send(new BulkNotificationActionCommand(request.Ids, request.Operation), ct);
        return HandleResult(result);
    }
}

public record BulkNotificationActionRequest(List<Guid> Ids, string Operation);
