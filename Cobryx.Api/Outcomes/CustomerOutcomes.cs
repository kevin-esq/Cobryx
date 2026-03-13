using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

public static class CustomerOutcomes
{
    private const string Prefix = "CRM.CUSTOMER";

    public static readonly Outcome Created = new($"{Prefix}.CREATED_SUCCESS", OutcomeCategory.Success, "Customer created successfully.");
    public static readonly Outcome Updated = new($"{Prefix}.UPDATED_SUCCESS", OutcomeCategory.Success, "Customer updated successfully.");
    public static readonly Outcome Deleted = new($"{Prefix}.DELETED_SUCCESS", OutcomeCategory.Success, "Customer deleted successfully.");
    public static readonly Outcome SearchCompleted = new($"{Prefix}.SEARCH_SUCCESS", OutcomeCategory.Success, "Customer search completed.");

    public static readonly Outcome ValidationFailed = new($"{Prefix}.VALIDATION_FAILED", OutcomeCategory.BusinessError, "Customer validation failed.");
    public static readonly Outcome Conflict = new($"{Prefix}.CONFLICT_ERROR", OutcomeCategory.BusinessError, "Customer conflict occurred.");
}
