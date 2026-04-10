using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Audit.Queries.GetAuditLogs;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Models;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Provides query access to the tenant's regulatory audit trail.
/// All mutations are recorded automatically — this controller exposes read-only access.
/// </summary>
[Authorize(Policy = "CanManageTenant")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit")]
[Tags("Operations")]
public class AuditController(ISender sender) : CobryxBaseController(sender)
{
    /// <summary>
    /// Queries the audit log with optional filters. Results are paginated and ordered by most recent first.
    /// </summary>
    /// <param name="page">Pagination index (1-based).</param>
    /// <param name="pageSize">Records per page (max 100).</param>
    /// <param name="entityName">Filter by entity type (e.g., "Customer", "Loan", "Invoice").</param>
    /// <param name="action">Filter by action (e.g., "Created", "Updated", "Deleted").</param>
    /// <param name="userId">Filter by the user who performed the action.</param>
    /// <param name="from">Filter entries created on or after this date (UTC).</param>
    /// <param name="to">Filter entries created on or before this date (UTC).</param>
    /// <remarks>
    /// Access Policy: Restricted to users with 'CanManageTenant' administrative permissions.
    ///
    /// Possible Outcomes:
    /// - AUDIT.SEARCH.COMPLETED: Audit entries retrieved successfully.
    /// </remarks>
    /// <response code="200">A paginated collection of audit log entries.</response>
    [HttpGet]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiSuccessResponse<PaginatedList<AuditLogEntry>>), 200)]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiErrorResponse), 403)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? entityName = null,
        [FromQuery] string? action = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var clampedPageSize = Math.Min(pageSize, 100);

        Result<PaginatedList<AuditLogEntry>> result = await Sender.Send(new GetAuditLogsQuery(
            page, clampedPageSize, entityName, action, userId, from, to));

        return HandleResult(result, AuditOutcomes.SearchCompleted);
    }
}
