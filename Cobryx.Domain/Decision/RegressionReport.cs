using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Decision;

public class RegressionReport : BaseEntity
{
    public DateTime RunAt { get; private set; } = DateTime.UtcNow;

    public int TotalProcessed { get; private set; }
    public int PassedCount { get; private set; }
    public int FailedCount { get; private set; }
    public int NonComparableCount { get; private set; }

    public decimal MeanLimitDrift { get; private set; }
    public decimal P95LimitDrift { get; private set; }
    public decimal MaxLimitDrift { get; private set; }

    public string TargetEngineVersion { get; private set; } = string.Empty;
    public string? BaselineEngineVersion { get; private set; }
    public string? ParentEngineVersion { get; private set; }
    
    public int SampleRate { get; private set; }
    public int SampleSize { get; private set; }
    public string? DatasetHash { get; private set; }

    /// <summary>
    /// JSON blob of the top 50 failing snapshots for deep-dive analysis.
    /// </summary>
    public string TopFailuresJson { get; private set; } = "[]";

    /// <summary>
    /// JSON blob of the severity distribution buckets.
    /// </summary>
    public string SeverityDistributionJson { get; private set; } = "{}";

    private RegressionReport() { }

    public RegressionReport(
        int total, int passed, int failed, int nonComparable,
        decimal mean, decimal p95, decimal max,
        string targetVersion, string? baselineVersion, string? parentVersion,
        int sampleRate, int sampleSize, string? datasetHash,
        string topFailuresJson, string severityJson)
    {
        TotalProcessed = total;
        PassedCount = passed;
        FailedCount = failed;
        NonComparableCount = nonComparable;
        MeanLimitDrift = mean;
        P95LimitDrift = p95;
        MaxLimitDrift = max;
        TargetEngineVersion = targetVersion;
        BaselineEngineVersion = baselineVersion;
        ParentEngineVersion = parentVersion;
        SampleRate = sampleRate;
        SampleSize = sampleSize;
        DatasetHash = datasetHash;
        TopFailuresJson = topFailuresJson;
        SeverityDistributionJson = severityJson;
        RunAt = DateTime.UtcNow;
    }
}
