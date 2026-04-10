using Asp.Versioning;

using Cobryx.Api.Outcomes;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for managing customer support operations, technical assistance tickets, and operational feedback.
/// Orchestrates the communication between end-users and the tenant support infrastructure.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/support/tickets")]
[Tags("Operations")]
public class SupportController(ISender sender) : CobryxBaseController(sender)
{

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
    [HttpPost]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> Create([FromBody] CreateSupportTicketRequest request)
    {
        var command = new Application.Support.Commands.Create.CreateSupportTicketCommand(
            request.Subject,
            request.Description,
            request.Priority,
            request.Category);

        var result = await Sender.Send(command);
        return HandleCreatedResult($"/api/v1/support/tickets/{result.Value}", result, SupportOutcomes.TicketCreated);
    }
}
