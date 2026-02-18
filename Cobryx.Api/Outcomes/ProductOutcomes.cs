using Cobryx.Domain.Common;

namespace Cobryx.Api.Outcomes;

public static class ProductOutcomes
{
    private const string Prefix = "PRODUCT";

    public static readonly Outcome Created = new($"{Prefix}.CREATED_SUCCESS", OutcomeCategory.Success, "Product created successfully.");
    public static readonly Outcome Updated = new($"{Prefix}.UPDATED_SUCCESS", OutcomeCategory.Success, "Product updated successfully.");
    public static readonly Outcome Deleted = new($"{Prefix}.DELETED_SUCCESS", OutcomeCategory.Success, "Product deleted successfully.");
    public static readonly Outcome SearchCompleted = new($"{Prefix}.SEARCH_SUCCESS", OutcomeCategory.Success, "Product search completed.");

    public static readonly Outcome ValidationFailed = new($"{Prefix}.VALIDATION_FAILED", OutcomeCategory.BusinessError, "Product validation failed.");
    public static readonly Outcome Conflict = new($"{Prefix}.CONFLICT_ERROR", OutcomeCategory.BusinessError, "Product conflict occurred.");
}
