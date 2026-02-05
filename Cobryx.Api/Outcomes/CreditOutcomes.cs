namespace Cobryx.Api.Outcomes;

public static class CreditOutcomes
{
    private const string Prefix = "CREDIT";

    public const string Created = $"{Prefix}.CREATED";
    public const string ScheduleGenerated = $"{Prefix}.SCHEDULE.GENERATED";
    public const string Liquidated = $"{Prefix}.LIQUIDATED";
    public const string SearchCompleted = $"{Prefix}.SEARCH.COMPLETED";
}
