namespace Cobryx.Api.Outcomes;

public static class CustomerOutcomes
{
    private const string Prefix = "CRM.CUSTOMER";

    public const string Created = $"{Prefix}.CREATED_SUCCESS";
    public const string Updated = $"{Prefix}.UPDATED_SUCCESS";
    public const string Deleted = $"{Prefix}.DELETED_SUCCESS";
    public const string SearchCompleted = $"{Prefix}.SEARCH_SUCCESS";

    public const string ValidationFailed = $"{Prefix}.VALIDATION_FAILED";
    public const string Conflict = $"{Prefix}.CONFLICT_ERROR";
}
