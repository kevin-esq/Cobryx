using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

public static class UserOutcomes
{
    private const string Prefix = "USER";

    public static readonly Outcome Updated = new($"{Prefix}.UPDATED_SUCCESS", OutcomeCategory.Success,
        "User updated successfully.");

    public static readonly Outcome SearchCompleted =
        new($"{Prefix}.SEARCH_SUCCESS", OutcomeCategory.Success, "User search completed.");

    public static readonly Outcome RolesSearchCompleted = new($"{Prefix}.ROLES_SEARCH_SUCCESS",
        OutcomeCategory.Success, "Available roles retrieved.");

    public static readonly Outcome ProfileRetrieved = new($"{Prefix}.PROFILE_RETRIEVED", OutcomeCategory.Success,
        "User profile retrieved successfully.");

    public static readonly Outcome ProfileUpdated = new($"{Prefix}.PROFILE_UPDATE_SUCCESS", OutcomeCategory.Success,
        "User profile updated successfully.");
}
