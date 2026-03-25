namespace Cobryx.Application.ML.Models;

public class ReplayResult
{
    public bool IsDeterministic { get; set; }
    public decimal DeltaCreditLimit { get; set; }
    public decimal DeltaInterestRate { get; set; }
    public string OriginalEngineVersion { get; set; } = string.Empty;
    public string ReplayedEngineVersion { get; set; } = string.Empty;
    public string OriginalTraceHash { get; set; } = string.Empty;
    public string ReplayedTraceHash { get; set; } = string.Empty;
    public bool TraceDriftDetected { get; set; }
    public TraceDiff? Diff { get; set; }
}

public class TraceDiff
{
    public List<StepDiff> Mismatches { get; set; } = new();
}

public class StepDiff
{
    public string StepName { get; set; } = string.Empty;
    public decimal OriginalOutput { get; set; }
    public decimal ReplayedOutput { get; set; }
    public string DescriptionMismatch { get; set; } = string.Empty;
}
