using System.Security.Cryptography;
using System.Text;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML;
using Cobryx.Application.ML.Models;
using Cobryx.Domain.Decision;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Decision
{
    public class SnapshotRegressionRunner(
        ICobryxDbContext db,
        ReplayEngine replayEngine)
    {
        public async Task<RegressionSuiteResult> RunRegressionAsync(
            DriftToleranceProfile profile,
            string? baselineVersion = null,
            int sampleRate = 100,
            int? earlyStopThreshold = null,
            CancellationToken ct = default)
        {
            var report = new RegressionSuiteResult();

            IQueryable<ProductionSnapshot> snapshots = db.ProductionSnapshots
                .Where(x => x.IsFullSnapshot)
                .AsNoTracking();

            var orderedSnapshots = snapshots.OrderBy(s => s.HashedCustomerId);

            var results = new List<RegressionResult>();
            var totalProcessed = 0;

            await foreach (var snapshot in orderedSnapshots.AsAsyncEnumerable().WithCancellation(ct))
            {
                if (!IsIncludedInSample(snapshot.HashedCustomerId, sampleRate))
                {
                    continue;
                }

                totalProcessed++;
                var input = new ProductionSnapshotAdapter(snapshot);

                if (baselineVersion != null && snapshot.EngineVersion == baselineVersion &&
                    snapshot.ConfigHash != EngineMetadata.ConfigHash)
                {
                    report.NonComparableCount++;
                    results.Add(new RegressionResult
                    {
                        SnapshotId = snapshot.Id,
                        IsComparable = false,
                        NonComparableReason = "ConfigHash mismatch against baseline"
                    });
                    continue;
                }

                var replayResult = await replayEngine.ReplayAsync(input);
                var result = MapToRegressionResult(snapshot.Id, replayResult, profile);

                results.Add(result);
                UpdateAggregateMetrics(report, result);

                if (earlyStopThreshold.HasValue && report.FailedCount >= earlyStopThreshold.Value)
                {
                    break;
                }

                if (result.OverallSeverity == DriftSeverity.Critical && earlyStopThreshold.HasValue)
                {
                    break;
                }
            }

            report.TotalProcessed = totalProcessed;
            report.SampleRate = sampleRate;
            report.DatasetHash = CalculateDatasetHash(results);
            report.ParentEngineVersion = baselineVersion;

            report.TopFailures =
            [
                .. results
                    .Where(r => r.OverallSeverity > DriftSeverity.None)
                    .OrderByDescending(r => r.OverallSeverity)
                    .ThenByDescending(r => Math.Abs(r.LimitDrift))
                    .Take(50)
            ];

            report.SeverityDistribution = results
                .GroupBy(r => r.OverallSeverity)
                .ToDictionary(g => g.Key, g => g.Count());

            return report;
        }

        private static bool IsIncludedInSample(string hashedId, int rate)
        {
            if (rate >= 100)
            {
                return true;
            }

            if (rate <= 0)
            {
                return false;
            }

            var bytes = Encoding.UTF8.GetBytes(hashedId);
            var hash = SHA256.HashData(bytes);
            var bucket = hash[0] % 100;
            return bucket < rate;
        }

        private static RegressionResult MapToRegressionResult(Guid id, ReplayResult replay,
            DriftToleranceProfile profile)
        {
            var result = new RegressionResult
            {
                SnapshotId = id,
                LimitDrift = replay.DeltaCreditLimit,
                RateDrift = replay.DeltaInterestRate,
                TraceDriftDetected = replay.TraceDriftDetected
            };

            result.OutputSeverity = ClassifyOutputSeverity(result, profile);
            result.TraceSeverity = replay.TraceDriftDetected ? DriftSeverity.Significant : DriftSeverity.None;

            if (replay.Diff != null && replay.Diff.Mismatches.Count != 0)
            {
                result.Attribution = PerformDriftAttribution(replay.Diff);
                if (result.Attribution.MaxImpactDelta > profile.CreditLimitAbsoluteThreshold * 2)
                {
                    result.TraceSeverity = DriftSeverity.Critical;
                }
            }

            return result;
        }

        private static DriftSeverity ClassifyOutputSeverity(RegressionResult result, DriftToleranceProfile profile)
        {
            var absLimit = Math.Abs(result.LimitDrift);
            var absRate = Math.Abs(result.RateDrift);

            return absLimit > profile.CreditLimitAbsoluteThreshold * 5 ||
                   absRate > profile.InterestRateRelativeThreshold * 5
                ? DriftSeverity.Critical
                : absLimit > profile.CreditLimitAbsoluteThreshold || absRate > profile.InterestRateRelativeThreshold
                    ? DriftSeverity.Significant
                    : absLimit > 0 || absRate > 0
                        ? DriftSeverity.Minor
                        : DriftSeverity.None;
        }

        private static DriftAttribution PerformDriftAttribution(TraceDiff diff)
        {
            var attr = new DriftAttribution();
            if (diff.Mismatches.Count == 0)
            {
                return attr;
            }

            attr.FirstDriftStep = diff.Mismatches.First().StepName;

            StepDiff maxMismatch = diff.Mismatches
                .OrderByDescending(static m => Math.Abs(m.ReplayedOutput - m.OriginalOutput))
                .First();

            attr.MaxImpactStep = maxMismatch.StepName;
            attr.MaxImpactDelta = Math.Abs(maxMismatch.ReplayedOutput - maxMismatch.OriginalOutput);
            attr.TotalImpact = diff.Mismatches.Sum(static m => Math.Abs(m.ReplayedOutput - m.OriginalOutput));

            return attr;
        }

        private static void UpdateAggregateMetrics(RegressionSuiteResult report, RegressionResult result)
        {
            if (result.OverallSeverity == DriftSeverity.None)
            {
                report.PassedCount++;
            }
            else
            {
                report.FailedCount++;
            }

            if (!result.IsComparable)
            {
                return;
            }

            report.MaxLimitDrift = Math.Max(report.MaxLimitDrift, Math.Abs(result.LimitDrift));
            report.MeanLimitDrift =
                ((report.MeanLimitDrift * (report.TotalProcessed - 1)) + Math.Abs(result.LimitDrift)) /
                Math.Max(1, report.TotalProcessed);
        }

        private static string CalculateDatasetHash(List<RegressionResult> results)
        {
            var sb = new StringBuilder();
            foreach (var r in results.OrderBy(r => r.SnapshotId))
            {
                sb.Append(r.SnapshotId.ToString());
            }
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return Convert.ToHexString(SHA256.HashData(bytes));
        }
    }
}
