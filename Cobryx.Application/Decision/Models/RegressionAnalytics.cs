using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision.Models
{

    public class DriftToleranceProfile
    {
        public decimal CreditLimitAbsoluteThreshold { get; set; } = 10.0m;
        public decimal InterestRateRelativeThreshold { get; set; } = 0.01m;
        public decimal ProbabilityAbsoluteThreshold { get; set; } = 0.001m;

        public decimal CriticalDriftRateThreshold { get; set; } = 0.05m;
    }

    public class DriftAttribution
    {
        public string FirstDriftStep { get; set; } = string.Empty;
        public string MaxImpactStep { get; set; } = string.Empty;
        public decimal MaxImpactDelta { get; set; }
        public decimal TotalImpact { get; set; }
    }

    public class RegressionResult
    {
        public Guid SnapshotId { get; set; }
        public bool IsComparable { get; set; } = true;
        public string? NonComparableReason { get; set; }

        public decimal LimitDrift { get; set; }
        public decimal RateDrift { get; set; }
        public bool TraceDriftDetected { get; set; }

        public DriftSeverity OutputSeverity { get; set; }
        public DriftSeverity TraceSeverity { get; set; }
        public DriftSeverity OverallSeverity => (DriftSeverity)Math.Max((int)OutputSeverity, (int)TraceSeverity);

        public DriftAttribution? Attribution { get; set; }
    }

    public class RegressionSuiteResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int TotalProcessed { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int NonComparableCount { get; set; }

        public double ComparableRatio =>
            TotalProcessed > 0 ? (double)(TotalProcessed - NonComparableCount) / TotalProcessed : 0;

        public double DriftRate => TotalProcessed > NonComparableCount
            ? (double)FailedCount / (TotalProcessed - NonComparableCount)
            : 0;

        public decimal MeanLimitDrift { get; set; }
        public decimal P95LimitDrift { get; set; }
        public decimal MaxLimitDrift { get; set; }
        public string? DatasetHash { get; set; }
        public int SampleRate { get; set; }
        public int SampleSize => TotalProcessed;
        public string? ParentEngineVersion { get; set; }

        public List<RegressionResult> TopFailures { get; set; } = [];
        public Dictionary<DriftSeverity, int> SeverityDistribution { get; set; } = [];
    }
}
