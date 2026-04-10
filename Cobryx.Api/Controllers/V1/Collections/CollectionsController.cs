using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Collections.Queries.GetPriorityCases;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1.Collections;

/// <summary>
/// Provides access to the collections priority queue and case management.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/collections")]
[Authorize]
[Tags("Collections")]
public class CollectionsController(ISender sender) : CobryxBaseController(sender)
{
    /// <summary>
    /// Returns the top priority collection cases from the Redis queue.
    /// </summary>
    /// <param name="limit">Maximum number of cases to return (default 100).</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Cases are sorted by priority score (highest first).
    /// Served from Redis with O(log n) complexity.
    ///
    /// Possible Outcomes:
    /// - COLLECTIONS.CASES.RETRIEVED: Priority cases retrieved successfully.
    /// </remarks>
    /// <response code="200">List of priority collection cases.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    [HttpGet("cases")]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<PriorityCaseDto>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetPriorityCases([FromQuery] int limit = 100, CancellationToken ct = default)
    {
        Result<List<PriorityCaseDto>> result = await Sender.Send(new GetPriorityCasesQuery(limit), ct);
        return HandleResult(result, CollectionsOutcomes.CasesRetrieved);
    }
}
