using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

/// <summary>
/// Status codes for the SRE &amp; System domain operations (Contract Level).
/// </summary>
public static class SystemOutcomes
{
    public static class Idempotency
    {
        private const string Prefix = "SYSTEM.IDEMPOTENCY";
        public static readonly Outcome IntrospectionRetrieved = new($"{Prefix}.INTROSPECT_SUCCESS", OutcomeCategory.Success, "Idempotency key introspected successfully.");
    }

    public static class Webhooks
    {
        private const string Prefix = "SYSTEM.WEBHOOK";
        public static readonly Outcome LogsRetrieved = new($"{Prefix}.LOGS_RETRIEVED", OutcomeCategory.Success, "Webhook logs retrieved successfully.");
        public static readonly Outcome Replayed = new($"{Prefix}.REPLAY_SUCCESS", OutcomeCategory.Success, "Webhook replayed successfully.");
        public static readonly Outcome ReplayForced = new($"{Prefix}.REPLAY_FORCED", OutcomeCategory.Warning, "Forced webhook replay executed.");
    }

    public static class Ledger
    {
        private const string Prefix = "SYSTEM.LEDGER";
        public static readonly Outcome IntegrityCheckCompleted = new($"{Prefix}.INTEGRITY_CHECK_SUCCESS", OutcomeCategory.Success, "Ledger integrity check completed.");
        public static readonly Outcome IntegrityCorruptionDetected = new($"{Prefix}.INTEGRITY_CORRUPTION", OutcomeCategory.Critical, "Ledger integrity corruption detected.");
    }
}
