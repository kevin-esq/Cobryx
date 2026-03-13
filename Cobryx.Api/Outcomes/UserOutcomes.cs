using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

public static class UserOutcomes
{
    private const string Prefix = "USER";

    public static readonly Outcome Created = new($"{Prefix}.CREATED_SUCCESS", OutcomeCategory.Success, "User created successfully.");
    public static readonly Outcome Updated = new($"{Prefix}.UPDATED_SUCCESS", OutcomeCategory.Success, "User updated successfully.");
    public static readonly Outcome ProfileUpdated = new($"{Prefix}.PROFILE_UPDATE_SUCCESS", OutcomeCategory.Success, "User profile updated successfully.");
    public static readonly Outcome Deleted = new($"{Prefix}.DELETED_SUCCESS", OutcomeCategory.Success, "User deleted successfully.");
    public static readonly Outcome SearchCompleted = new($"{Prefix}.SEARCH_SUCCESS", OutcomeCategory.Success, "User search completed.");

    public static readonly Outcome ValidationFailed = new($"{Prefix}.VALIDATION_FAILED", OutcomeCategory.BusinessError, "User validation failed.");
}
