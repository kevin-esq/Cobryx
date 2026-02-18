using Cobryx.Domain.Common;

namespace Cobryx.Api.Outcomes;

public static class AuditOutcomes
{
    private const string Prefix = "AUDIT";

    public static readonly Outcome Created = new($"{Prefix}.CREATED_SUCCESS", OutcomeCategory.Success, "Audit log created successfully.");
    public static readonly Outcome SearchCompleted = new($"{Prefix}.SEARCH_SUCCESS", OutcomeCategory.Success, "Audit log search completed.");
}
