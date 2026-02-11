namespace Cobryx.Api.Outcomes;

public static class CreditOutcomes
{
    private const string Prefix = "CREDIT";

    public const string Created = $"{Prefix}.CREATED";
    public const string ScheduleGenerated = $"{Prefix}.SCHEDULE.GENERATED";
    public const string Liquidated = $"{Prefix}.LIQUIDATED";
    public const string SearchCompleted = $"{Prefix}.SEARCH.COMPLETED";

    public const string ValidationFailed = $"{Prefix}.VALIDATION_FAILED";
    public const string PolicyViolation = $"{Prefix}.POLICY_VIOLATION";
    public const string Conflict = $"{Prefix}.CONFLICT";
}
