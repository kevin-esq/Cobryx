using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

public static class AdminOutcomes
{
    private const string Prefix = "ADMIN";

    public static class Health
    {
        public static readonly Outcome LedgerHealthRetrieved = new($"{Prefix}.HEALTH.LEDGER_RETRIEVED", OutcomeCategory.Success, "Ledger health status retrieved.");
    }

    public static class Metrics
    {
        public static readonly Outcome Retrieved = new($"{Prefix}.METRICS.RETRIEVED", OutcomeCategory.Success, "Financial metrics retrieved.");
    }

    public static class Reconciliation
    {
        public static readonly Outcome StripeRetrieved = new($"{Prefix}.RECONCILIATION.STRIPE_RETRIEVED", OutcomeCategory.Success, "Stripe reconciliation data retrieved.");
    }

    public static class Tenant
    {
        public static readonly Outcome Suspended = new($"{Prefix}.TENANT.SUSPENDED", OutcomeCategory.Success, "Tenant suspended successfully.");
    }

    public static class Transaction
    {
        public static readonly Outcome Reversed = new($"{Prefix}.TRANSACTION.REVERSED", OutcomeCategory.Success, "Transaction reversed successfully.");
    }

    public static class Loan
    {
        public static readonly Outcome ChargedOff = new($"{Prefix}.LOAN.CHARGED_OFF", OutcomeCategory.Success, "Loan charged off successfully.");
    }

    public static class Alert
    {
        public static readonly Outcome TestSent = new($"{Prefix}.ALERT.TEST_SENT", OutcomeCategory.Success, "Test alert dispatched.");
    }
}
