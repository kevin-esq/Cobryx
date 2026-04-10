using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision;
using Cobryx.Application.Decision.Models;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs.Decision
{
    public class RegressionSuiteJob(
        SnapshotRegressionRunner runner,
        ICobryxDbContext db,
        ILogger<RegressionSuiteJob> logger)
    {
        public async Task RunAsync(CancellationToken ct)
        {
            logger.LogInformation("Nightly Regression Suite: Starting.");

            try
            {
                var profile = new DriftToleranceProfile();

                var result = await runner.RunRegressionAsync(
                    profile,
                    sampleRate: 100,
                    ct: ct);

                var dbReport = new Domain.Decision.RegressionReport(
                    result.TotalProcessed,
                    result.PassedCount,
                    result.FailedCount,
                    result.NonComparableCount,
                    result.MeanLimitDrift,
                    result.P95LimitDrift,
                    result.MaxLimitDrift,
                    Domain.Decision.EngineMetadata.EngineVersion,
                    null,
                    null,
                    result.SampleRate,
                    result.SampleSize,
                    result.DatasetHash,
                    JsonSerializer.Serialize(result.TopFailures),
                    JsonSerializer.Serialize(result.SeverityDistribution)
                );

                _ = db.RegressionReports.Add(dbReport);
                _ = await db.SaveChangesAsync(ct);

                logger.LogInformation("Nightly Regression Suite: Completed. Drift Rate: {DriftRate:P2}",
                    result.DriftRate);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Nightly Regression Suite: Failed");
            }
        }
    }
}
