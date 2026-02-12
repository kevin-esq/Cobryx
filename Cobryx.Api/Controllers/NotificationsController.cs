using Cobryx.Domain.Entities;
using Cobryx.Domain.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Api.Controllers;

[Authorize] // Any authenticated user can see their own/tenant notifications
[Route("api/notifications")]
public class NotificationsController : CobryxBaseController
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationsController(ISender sender, ITenantProvider tenantProvider, IUnitOfWork unitOfWork)
        : base(sender)
    {
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
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

        return Success(notifications);
    }

    [HttpPatch("{id:guid}/read")]
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

    [HttpPost("read-all")]
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
