using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using CreateTicketCommand = Cobryx.Application.Support.Commands.Create.CreateSupportTicketCommand;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Manages customer support operations, technical assistance tickets, and operational feedback.
/// Orchestrates communication between end-users and the tenant support infrastructure.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/support/tickets")]
[Tags("Operations")]
public class SupportController(ISender sender) : CobryxBaseController(sender)
{
    private const string TicketsBasePath = "/api/v1/support/tickets";

    /// <summary>
    /// Submits a new technical or operational support ticket.
    /// </summary>
    /// <param name="request">Ticket details including subject, category, and priority level.</param>
    /// <remarks>
    /// Tickets are automatically associated with the authenticated user context.
    ///
    /// Possible Outcomes:
    /// - SUPPORT.TICKET.CREATED: Ticket successfully queued for triage.
    /// - SUPPORT.TICKET.FAILED: Validation failed (e.g., empty subject or invalid category).
    /// </remarks>
    /// <response code="201">Returns the unique identifier for the submitted support ticket.</response>
    /// <response code="400">Malformed request or invalid priority levels.</response>
    /// <response code="401">Unauthorized.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateSupportTicketRequest request)
    {
        var command = new CreateTicketCommand(
            request.Subject,
            request.Description,
            request.Priority,
            request.Category);

        Result<Guid> result = await Sender.Send(command);

        return HandleCreatedResult($"{TicketsBasePath}/{result.Value}", result, SupportOutcomes.TicketCreated);
    }
}
