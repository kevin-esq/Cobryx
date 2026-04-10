using Cobryx.Application.Decision.Interfaces;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML.Models;
using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision;

public class ShadowComparer : IShadowComparer
{
    public ShadowExecutionResult Compare(Guid snapshotId, ReplayResult primary, ReplayResult shadow, DriftToleranceProfile profile)
    {
        var result = new ShadowExecutionResult
        {
            SnapshotId = snapshotId,
            Primary = primary,
            Shadow = shadow
        };

        result.OutputSeverity = ClassifyOutputSeverity(result, profile);
        result.TraceSeverity = shadow.TraceDriftDetected ? DriftSeverity.Significant : DriftSeverity.None;

        if (shadow.Diff != null && shadow.Diff.Mismatches.Count != 0)
        {
            result.Attribution = PerformDriftAttribution(shadow.Diff);
            if (result.Attribution.MaxImpactDelta > profile.CreditLimitAbsoluteThreshold * 2)
            {
                result.TraceSeverity = DriftSeverity.Critical;
            }
        }

        return result;
    }

    private static DriftSeverity ClassifyOutputSeverity(ShadowExecutionResult result, DriftToleranceProfile profile)
    {
        var absLimit = Math.Abs(result.DeltaLimit);
        var absRate = Math.Abs(result.DeltaRate);

        return absLimit > profile.CreditLimitAbsoluteThreshold * 5 || absRate > profile.InterestRateRelativeThreshold * 5
            ? DriftSeverity.Critical
            : absLimit > profile.CreditLimitAbsoluteThreshold || absRate > profile.InterestRateRelativeThreshold
                ? DriftSeverity.Significant
                : absLimit > 0 || absRate > 0 ? DriftSeverity.Minor : DriftSeverity.None;
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
