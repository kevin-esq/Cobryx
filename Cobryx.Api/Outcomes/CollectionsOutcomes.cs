using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

public static class CollectionsOutcomes
{
    private const string Prefix = "COLLECTIONS";

    public static readonly Outcome CasesRetrieved = new($"{Prefix}.CASES.RETRIEVED", OutcomeCategory.Success, "Priority cases retrieved successfully.");
    public static readonly Outcome CaseAssigned = new($"{Prefix}.CASE.ASSIGNED", OutcomeCategory.Success, "Case assigned to collector.");
    public static readonly Outcome ActionRecorded = new($"{Prefix}.ACTION.RECORDED", OutcomeCategory.Success, "Collection action recorded.");
}
