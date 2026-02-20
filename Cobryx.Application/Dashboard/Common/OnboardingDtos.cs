namespace Cobryx.Application.Dashboard.Common;

public record OnboardingStatusDto(
    bool BusinessCompleted,
    bool FirstUserInvited,
    bool FirstLoanCreated,
    bool FirstPaymentRegistered,
    bool HasSeenValueHabit,
    int ProgressPercentage,
    OnboardingMilestone CurrentMilestone,
    bool IsDemoProgress
);

public record OnboardingContextDto(
    int ActiveLoans,
    int Users,
    int PaymentsLast30d,
    bool HasSeenDashboardWithData
);

public record NextBestActionDto(
    OnboardingAction Action,
    OnboardingReason Reason,
    int Priority,
    OnboardingContextDto Context
);
