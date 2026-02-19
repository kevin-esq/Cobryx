using Cobryx.Domain.Common;

namespace Cobryx.Application.Tenants.Common;

public static class InvitationOutcomes
{
    private const string Prefix = "TENANT.INVITATION";

    public static readonly Outcome InviteSuccess = new($"{Prefix}.INVITE_SUCCESS", OutcomeCategory.Success, "Invitation sent successfully.");
    public static readonly Outcome EnrollSuccess = new($"{Prefix}.ENROLL_SUCCESS", OutcomeCategory.Success, "User enrolled successfully.");
    public static readonly Outcome RevokeSuccess = new($"{Prefix}.REVOKE_SUCCESS", OutcomeCategory.Success, "Invitation revoked successfully.");
    public static readonly Outcome FetchSuccess = new($"{Prefix}.FETCH_SUCCESS", OutcomeCategory.Success, "Invitations retrieved successfully.");
}
