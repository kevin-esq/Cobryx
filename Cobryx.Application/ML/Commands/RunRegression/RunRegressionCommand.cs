using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision;
using Cobryx.Application.Decision.Models;
using Cobryx.Domain.Decision;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.ML.Commands.RunRegression;

public record RunRegressionCommand(
    DriftToleranceProfile? Profile,
    string? BaselineVersion,
    int SampleRate = 100,
    int? EarlyStopThreshold = null) : IRequest<Result<RegressionSuiteResultDto>>;

public record RegressionSuiteResultDto(
    Guid Id,
    int TotalProcessed,
    double DriftRate,
    double ComparableRatio,
    int PassedCount,
    int FailedCount,
    int NonComparableCount,
    Dictionary<DriftSeverity, int> SeverityDistribution);

public partial class RunRegressionHandler(
    SnapshotRegressionRunner runner,
    ICobryxDbContext db,
    ILogger<RunRegressionHandler> logger) : IRequestHandler<RunRegressionCommand, Result<RegressionSuiteResultDto>>
{
    public async Task<Result<RegressionSuiteResultDto>> Handle(RunRegressionCommand request, CancellationToken cancellationToken)
    {
        LogRegressionRequested(logger, request.SampleRate);

        DriftToleranceProfile profile = request.Profile ?? new DriftToleranceProfile();

        RegressionSuiteResult suiteResult = await runner.RunRegressionAsync(
            profile,
            request.BaselineVersion,
            request.SampleRate,
            request.EarlyStopThreshold,
            cancellationToken);

        var dbReport = new RegressionReport(
            suiteResult.TotalProcessed,
            suiteResult.PassedCount,
            suiteResult.FailedCount,
            suiteResult.NonComparableCount,
            suiteResult.MeanLimitDrift,
            suiteResult.P95LimitDrift,
            suiteResult.MaxLimitDrift,
            EngineMetadata.EngineVersion,
            request.BaselineVersion,
            suiteResult.ParentEngineVersion,
            suiteResult.SampleRate,
            suiteResult.SampleSize,
            suiteResult.DatasetHash,
            JsonSerializer.Serialize(suiteResult.TopFailures),
            JsonSerializer.Serialize(suiteResult.SeverityDistribution));

        db.RegressionReports.Add(dbReport);
        await db.SaveChangesAsync(cancellationToken);

        var dto = new RegressionSuiteResultDto(
            suiteResult.Id,
            suiteResult.TotalProcessed,
            suiteResult.DriftRate,
            suiteResult.ComparableRatio,
            suiteResult.PassedCount,
            suiteResult.FailedCount,
            suiteResult.NonComparableCount,
            suiteResult.SeverityDistribution);

        return Result.Success(dto);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Regression suite requested with sample rate {SampleRate}")]
    private static partial void LogRegressionRequested(ILogger logger, int sampleRate);
}
