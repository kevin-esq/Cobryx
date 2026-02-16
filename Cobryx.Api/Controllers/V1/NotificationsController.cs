using Asp.Versioning;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Outcomes;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for managing system alerts and tenant notifications.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Tags("Platform")]
public class NotificationsController : CobryxBaseController
{
    private readonly Application.Common.Interfaces.ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationsController(ISender sender, Application.Common.Interfaces.ITenantProvider tenantProvider, IUnitOfWork unitOfWork)
        : base(sender)
    {
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Retrieves a list of notifications for the current tenant.
    /// </summary>
    /// <param name="unreadOnly">If true, only returns notifications that haven't been marked as read.</param>
    /// <param name="limit">Maximum number of results to return (default 20).</param>
    /// <response code="200">A collection of notifications.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<NotificationContract>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = true, [FromQuery] int limit = 20)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var dbContext = (DbContext)_unitOfWork;

        var query = dbContext.Set<Notification>()
            .Where(n => n.TenantId == tenantId && !n.IsDeleted);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();

        var mapped = notifications.Select(n => new NotificationContract(
            n.Id,
            n.Title,
            n.Message,
            n.Type.ToString(),
            n.IsRead,
            n.CreatedAt)).ToList();

        return Success(mapped);
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    /// <param name="id">Identifier of the notification.</param>
    /// <response code="204">Acknowledged.</response>
    /// <response code="404">Notification not found.</response>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var dbContext = (DbContext)_unitOfWork;

        var notification = await dbContext.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId);

        if (notification == null) return NotFound();

        notification.MarkAsRead();
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Marks all unread tenant notifications as read.
    /// </summary>
    /// <response code="204">Acknowledged.</response>
    [HttpPost("read-all")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var dbContext = (DbContext)_unitOfWork;

        var notifications = await dbContext.Set<Notification>()
            .Where(n => n.TenantId == tenantId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }

        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }
}
