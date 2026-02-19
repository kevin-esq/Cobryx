namespace Cobryx.Application.Dashboard.Common;

public enum OnboardingAction
{
    CompleteBusinessProfile,
    InviteFirstUser,
    CreateFirstLoan,
    RegisterFirstPayment,
    ViewDashboard,
    None
}

public enum OnboardingReason
{
    MissingBusinessData,
    NoUsers,
    NoLoans,
    NoPayments,
    NoDashboardData
}

public enum OnboardingMilestone
{
    EstablishingFoundation,
    BuildingTeam,
    CreatingAssets,
    RealizingValue,
    GrowthReady
}
