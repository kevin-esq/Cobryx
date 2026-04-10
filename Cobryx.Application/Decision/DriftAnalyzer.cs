using Cobryx.Application.Decision.Interfaces;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML.Models;
using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision;

public class DriftAnalyzer : IDriftAnalyzer
{
    public RegressionResult AnalyzeReplayResult(Guid snapshotId, ReplayResult replay, DriftToleranceProfile profile)
    {
        var result = new RegressionResult
        {
            SnapshotId = snapshotId,
            LimitDrift = replay.DeltaCreditLimit,
            RateDrift = replay.DeltaInterestRate,
            TraceDriftDetected = replay.TraceDriftDetected
        };

        result.OutputSeverity = ClassifyOutputSeverity(result, profile);
        result.TraceSeverity = replay.TraceDriftDetected ? DriftSeverity.Significant : DriftSeverity.None;

        if (replay.Diff == null || replay.Diff.Mismatches.Count == 0)
            return result;

        result.Attribution = PerformDriftAttribution(replay.Diff);
        if (result.Attribution.MaxImpactDelta > profile.CreditLimitAbsoluteThreshold * 2)
        {
            result.TraceSeverity = DriftSeverity.Critical;
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
            return attr;

        attr.FirstDriftStep = diff.Mismatches.First().StepName;
        var maxMismatch = diff.Mismatches.OrderByDescending(m => Math.Abs(m.ReplayedOutput - m.OriginalOutput)).First();
        attr.MaxImpactStep = maxMismatch.StepName;
        attr.MaxImpactDelta = Math.Abs(maxMismatch.ReplayedOutput - maxMismatch.OriginalOutput);
        attr.TotalImpact = diff.Mismatches.Sum(m => Math.Abs(m.ReplayedOutput - m.OriginalOutput));

        return attr;
    }
}
