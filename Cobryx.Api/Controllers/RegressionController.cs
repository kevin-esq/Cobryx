using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML.Commands.RunRegression;
using Cobryx.Application.ML.Queries.DebugReplay;
using Cobryx.Application.ML.Queries.GetRegressionReport;
using Cobryx.Application.ML.Queries.GetRegressionReports;
using Cobryx.Domain.Decision;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

/// <summary>
/// Provides ML model regression testing and drift analysis capabilities.
/// Used for validating model determinism across versions.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ml/regression")]
[Authorize(Roles = "Admin,Audit")]
[Tags("ML & Risk")]
public class RegressionController(ISender sender) : CobryxBaseController(sender)
{
    /// <summary>
    /// Executes a full regression suite against production snapshots.
    /// </summary>
    /// <param name="request">Regression configuration including tolerance profile and sample rate.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// This is a long-running operation that validates ML model determinism.
    /// Results are persisted for audit purposes.
    ///
    /// Possible Outcomes:
    /// - ML.REGRESSION.COMPLETED: Regression suite completed successfully.
    /// </remarks>
    /// <response code="200">Regression suite results with drift statistics.</response>
    [HttpPost("run")]
    [ProducesResponseType(typeof(ApiSuccessResponse<RegressionSuiteResultDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> Run([FromBody] RunRegressionRequest request, CancellationToken ct)
    {
        var command = new RunRegressionCommand(
            request.Profile,
            request.BaselineVersion,
            request.SampleRate,
            request.EarlyStopThreshold);

        Result<RegressionSuiteResultDto> result = await Sender.Send(command, ct);
        return HandleResult(result, MlOutcomes.Regression.Completed);
    }

    /// <summary>
    /// Returns the most recent regression reports.
    /// </summary>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - ML.REGRESSION.REPORTS_LISTED: Reports retrieved successfully.
    /// </remarks>
    /// <response code="200">List of recent regression reports.</response>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<RegressionReport>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> GetReports(CancellationToken ct)
    {
        Result<List<RegressionReport>> result = await Sender.Send(new GetRegressionReportsQuery(), ct);
        return HandleResult(result, MlOutcomes.Regression.ReportsListed);
    }

    /// <summary>
    /// Retrieves a specific regression report by ID.
    /// </summary>
    /// <param name="id">The report identifier.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - ML.REGRESSION.REPORT_RETRIEVED: Report found and returned.
    /// </remarks>
    /// <response code="200">The regression report details.</response>
    /// <response code="404">Report not found.</response>
    [HttpGet("reports/{id:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<RegressionReport>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetReport(Guid id, CancellationToken ct)
    {
        Result<RegressionReport> result = await Sender.Send(new GetRegressionReportQuery(id), ct);
        return HandleResult(result, MlOutcomes.Regression.ReportRetrieved);
    }

    /// <summary>
    /// Debug replay of a specific production snapshot with custom thresholds.
    /// </summary>
    /// <param name="id">The snapshot identifier.</param>
    /// <param name="limitThreshold">Credit limit drift threshold.</param>
    /// <param name="rateThreshold">Interest rate drift threshold.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - ML.REPLAY.COMPLETED: Debug replay completed.
    /// </remarks>
    /// <response code="200">Detailed replay result with drift attribution.</response>
    /// <response code="404">Snapshot not found.</response>
    [HttpGet("replay/{id:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<DebugReplayResultDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> DebugReplay(
        Guid id,
        [FromQuery] decimal limitThreshold = 10,
        [FromQuery] decimal rateThreshold = 0.01m,
        CancellationToken ct = default)
    {
        Result<DebugReplayResultDto> result = await Sender.Send(
            new DebugReplayQuery(id, limitThreshold, rateThreshold), ct);
        return HandleResult(result, MlOutcomes.Replay.Completed);
    }
}

public class RunRegressionRequest
{
    public DriftToleranceProfile? Profile { get; set; }
    public string? BaselineVersion { get; set; }
    public int SampleRate { get; set; } = 100;
    public int? EarlyStopThreshold { get; set; }
}
