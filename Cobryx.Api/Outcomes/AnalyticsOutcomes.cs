using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

public static class AnalyticsOutcomes
{
    private const string Prefix = "ANALYTICS";

    public static class Portfolio
    {
        public static readonly Outcome SummaryRetrieved = new($"{Prefix}.PORTFOLIO.SUMMARY_RETRIEVED", OutcomeCategory.Success, "Portfolio summary metrics retrieved.");
        public static readonly Outcome AgingRetrieved = new($"{Prefix}.PORTFOLIO.AGING_RETRIEVED", OutcomeCategory.Success, "Portfolio aging buckets retrieved.");
        public static readonly Outcome KpisRetrieved = new($"{Prefix}.PORTFOLIO.KPIS_RETRIEVED", OutcomeCategory.Success, "Core KPIs retrieved.");
    }
}
