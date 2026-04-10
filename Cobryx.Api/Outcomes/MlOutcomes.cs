using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

public static class MlOutcomes
{
    private const string Prefix = "ML";

    public static class Replay
    {
        public static readonly Outcome Completed = new($"{Prefix}.REPLAY.COMPLETED", OutcomeCategory.Success, "Replay execution completed successfully.");
        public static readonly Outcome DriftDetected = new($"{Prefix}.REPLAY.DRIFT_DETECTED", OutcomeCategory.Warning, "Replay completed but determinism drift was detected.");
    }

    public static class Regression
    {
        public static readonly Outcome Completed = new($"{Prefix}.REGRESSION.COMPLETED", OutcomeCategory.Success, "Regression suite completed successfully.");
        public static readonly Outcome ReportRetrieved = new($"{Prefix}.REGRESSION.REPORT_RETRIEVED", OutcomeCategory.Success, "Regression report retrieved.");
        public static readonly Outcome ReportsListed = new($"{Prefix}.REGRESSION.REPORTS_LISTED", OutcomeCategory.Success, "Regression reports listed.");
    }
}
