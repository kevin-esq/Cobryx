using System.Diagnostics.Metrics;
using System.Collections.Generic;
using System;
using Cobryx.Domain.Enums;

namespace Cobryx.Application.Common.Observability;

public sealed class CobryxMetrics : IDisposable
{
    public const string MeterName = "Cobryx.Api";

    private readonly Meter _meter;

    public Counter<long> BusinessOutcomes { get; }
    public Counter<long> CobryxOutcomeTotal { get; }
    public Counter<long> DomainErrors { get; }
    public Counter<long> LoginSuccesses { get; }
    public Counter<long> LoginFailures { get; }
    public Counter<long> TokenRefreshes { get; }
    public Counter<long> PasswordResets { get; }
    public Counter<long> OutboxJobsProcessed { get; }
    public Counter<long> RecoveryAttemptTotal { get; }
    public Counter<double> RecoveryRevenueTotal { get; }
    public Counter<long> RecoveryDisputeStopTotal { get; }

    // Invitation Metrics
    public Counter<long> InvitationsCreated { get; }
    public Counter<long> InvitationsAccepted { get; }
    public Counter<long> InvitationsExpired { get; }
    public Counter<long> InvitationsRejected { get; }
    public Counter<long> InvitationsReplayAttempts { get; }

    // Cleanup Metrics
    public Counter<long> CleanupInvitationsDeleted { get; }
    public Counter<long> CleanupInvitationsExpired { get; }

    // Monetization Metrics
    public Counter<long> SubscriptionLimitReached { get; }
    public Counter<long> SubscriptionUpgrades { get; }
    public Counter<long> SubscriptionDowngradeRejected { get; }

    // Subscription Gate Metrics
    public Counter<long> SubscriptionGateBlocked { get; }
    public Counter<long> SubscriptionGateCacheHits { get; }
    public Counter<long> SubscriptionGateCacheMisses { get; }

    // Document Scan Metrics
    public Counter<long> DocumentsScanTotal { get; }
    public Counter<long> DocumentsInfectedTotal { get; }
    public Counter<long> DocumentsScanFailTotal { get; }
    public Histogram<double> DocumentsScanLatency { get; }
    public Counter<long> ScannerCircuitBreakerTrips { get; }

    // Performance & Scaling Metrics
    public Counter<long> UsageCacheHits { get; }
    public Counter<long> UsageCacheMisses { get; }
    public Histogram<double> OutboxProcessingLag { get; }
    public Histogram<double> CommandDuration { get; }

    // Database Infrastructure Metrics
    public Histogram<double> DbCommandDuration { get; }
    public Counter<long> DbRetryTotal { get; }
    public Counter<long> DbPoolExhaustionTotal { get; }
    public Counter<long> DbCommandTimeoutTotal { get; }
    public UpDownCounter<long> ConcurrentDbCommands { get; }

    // Hangfire Infrastructure Metrics (Observable)
    public ObservableGauge<long> HangfireActiveWorkers { get; }
    public ObservableGauge<long> HangfireQueueLength { get; }
    public ObservableGauge<long> HangfireFailedJobs { get; }
    public ObservableGauge<long> HangfireDeletedJobs { get; }
    public Histogram<double> HangfireQueueLatency { get; }
    public Histogram<double> TimeToWow { get; }
    public Counter<long> FeatureActivation { get; }
    public Counter<long> OnboardingAbandoned { get; }
    public Counter<long> TrialExpiredNoWow { get; }
    public Counter<long> SlowActivation { get; }
    public Histogram<double> TimeToExpansion { get; }
    public ObservableGauge<double> RevenueConcentration { get; }

    private static Func<long> _activeWorkersProvider = () => 0;
    private static Func<long> _queueLengthProvider = () => 0;
    private static Func<long> _failedJobsProvider = () => 0;
    private static Func<long> _deletedJobsProvider = () => 0;
    private static Func<double> _revenueConcentrationProvider = () => 0;

    public static void RegisterHangfireProviders(
        Func<long> activeWorkers,
        Func<long> queueLength,
        Func<long> failedJobs,
        Func<long> deletedJobs)
    {
        _activeWorkersProvider = activeWorkers;
        _queueLengthProvider = queueLength;
        _failedJobsProvider = failedJobs;
        _deletedJobsProvider = deletedJobs;
    }

    public static void RegisterRevenueConcentrationProvider(Func<double> provider)
    {
        _revenueConcentrationProvider = provider;
    }

    public CobryxMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        BusinessOutcomes = _meter.CreateCounter<long>(
            "cobryx_business_outcomes_total",
            description: "Total number of successful business outcomes");

        CobryxOutcomeTotal = _meter.CreateCounter<long>(
            "cobryx_outcome_total",
            description: "Total number of business outcomes taged by code, success and tier");

        DomainErrors = _meter.CreateCounter<long>(
            "cobryx_domain_errors_total",
            description: "Total number of domain errors");

        LoginSuccesses = _meter.CreateCounter<long>("auth_login_success_total", description: "Total number of successful logins");
        LoginFailures = _meter.CreateCounter<long>("auth_login_failure_total", description: "Total number of failed logins");
        TokenRefreshes = _meter.CreateCounter<long>("auth_token_refresh_total", description: "Total number of token refresh operations");
        PasswordResets = _meter.CreateCounter<long>("auth_password_reset_total", description: "Total number of password resets requested");
        OutboxJobsProcessed = _meter.CreateCounter<long>("outbox_jobs_processed_total", description: "Total number of outbox jobs processed");
        RecoveryAttemptTotal = _meter.CreateCounter<long>("cobryx_recovery_attempt_total", description: "Total number of recovery attempts tagged by attempt number and outcome");
        RecoveryRevenueTotal = _meter.CreateCounter<double>("cobryx_recovery_revenue_total", unit: "$", description: "Total revenue successfully recovered by the dunning engine");
        RecoveryDisputeStopTotal = _meter.CreateCounter<long>("cobryx_recovery_dispute_stop_total", description: "Total recovery attempts aborted due to active disputes");

        // Monetization
        SubscriptionLimitReached = _meter.CreateCounter<long>("subscription_limit_reached_total", description: "Total hits to a plan limit");
        SubscriptionUpgrades = _meter.CreateCounter<long>("subscription_upgrade_total", description: "Total subscription upgrades");
        SubscriptionDowngradeRejected = _meter.CreateCounter<long>("subscription_downgrade_rejected_total", description: "Total rejected downgrades due to usage");

        // Subscription Gate
        SubscriptionGateBlocked = _meter.CreateCounter<long>("subscription_gate_blocked_total", description: "Total requests blocked by subscription gate");
        SubscriptionGateCacheHits = _meter.CreateCounter<long>("subscription_gate_cache_hit_total", description: "Cache hits for subscription status lookups");
        SubscriptionGateCacheMisses = _meter.CreateCounter<long>("subscription_gate_cache_miss_total", description: "Cache misses for subscription status lookups");

        // Document Scanning
        DocumentsScanTotal = _meter.CreateCounter<long>("documents_scan_total", description: "Total document scans completed");
        DocumentsInfectedTotal = _meter.CreateCounter<long>("documents_infected_total", description: "Total infected documents detected");
        DocumentsScanFailTotal = _meter.CreateCounter<long>("documents_scan_fail_total", description: "Total document scan failures");
        DocumentsScanLatency = _meter.CreateHistogram<double>("documents_scan_latency_seconds", unit: "s", description: "Duration of virus scans");
        ScannerCircuitBreakerTrips = _meter.CreateCounter<long>("scanner_circuit_breaker_trips_total", description: "Total times the scanner circuit breaker rejected a request");

        // Scaling
        UsageCacheHits = _meter.CreateCounter<long>("usage_cache_hit_total", description: "Total cache hits for usage snapshots");
        UsageCacheMisses = _meter.CreateCounter<long>("usage_cache_miss_total", description: "Total cache misses for usage snapshots");

        OutboxProcessingLag = _meter.CreateHistogram<double>("outbox_processing_lag_seconds", unit: "s", description: "Lag between event occurrence and processing");
        CommandDuration = _meter.CreateHistogram<double>("command_duration_seconds", unit: "s", description: "Duration of business commands");

        // Invitations
        InvitationsCreated = _meter.CreateCounter<long>("invitations_created_total", description: "Total invitations sent");
        InvitationsAccepted = _meter.CreateCounter<long>("invitations_accepted_total", description: "Total invitations accepted");
        InvitationsExpired = _meter.CreateCounter<long>("invitations_expired_total", description: "Total invitations expired");
        InvitationsRejected = _meter.CreateCounter<long>("invitations_rejected_total", description: "Total enrollment attempts rejected (invalid token/mismatch)");
        InvitationsReplayAttempts = _meter.CreateCounter<long>("invitations_replay_attempts_total", description: "Total attempts to use an already accepted/expired token");

        CleanupInvitationsDeleted = _meter.CreateCounter<long>("cleanup_invitations_deleted_total", description: "Total old invitations hard-deleted by cleanup job");
        CleanupInvitationsExpired = _meter.CreateCounter<long>("cleanup_invitations_expired_total", description: "Total stale invitations marked as expired by cleanup job");

        // Database Infrastructure
        DbCommandDuration = _meter.CreateHistogram<double>("db_command_duration_seconds", unit: "s", description: "Duration of database commands");
        DbRetryTotal = _meter.CreateCounter<long>("db_retry_total", description: "Total number of transient database error retries");
        DbPoolExhaustionTotal = _meter.CreateCounter<long>("db_pool_exhaustion_total", description: "Total number of connection pool exhaustion events");
        DbCommandTimeoutTotal = _meter.CreateCounter<long>("db_command_timeout_total", description: "Total number of database command timeouts");
        ConcurrentDbCommands = _meter.CreateUpDownCounter<long>("db_concurrent_commands", description: "Number of database commands currently executing");

        // Hangfire Infrastructure
        HangfireQueueLatency = _meter.CreateHistogram<double>("hangfire_queue_latency_seconds", unit: "s", description: "Time background jobs spend in queue");

        HangfireActiveWorkers = _meter.CreateObservableGauge<long>("hangfire_active_workers",
            () => _activeWorkersProvider(), description: "Number of active Hangfire workers");

        HangfireQueueLength = _meter.CreateObservableGauge<long>("hangfire_queue_length",
            () => _queueLengthProvider(), description: "Number of jobs waiting in Hangfire queues");

        HangfireFailedJobs = _meter.CreateObservableGauge<long>("hangfire_failed_jobs_total",
            () => _failedJobsProvider(), description: "Total number of failed background jobs");

        HangfireDeletedJobs = _meter.CreateObservableGauge<long>("hangfire_deleted_jobs_total",
            () => _deletedJobsProvider(), description: "Total number of deleted background jobs");

        TimeToWow = _meter.CreateHistogram<double>("cobryx_time_to_wow_seconds", unit: "s", description: "Time from tenant creation to first value realization (Wow)");
        FeatureActivation = _meter.CreateCounter<long>("cobryx_feature_activation_total", description: "Intensity of feature usage across tiers");
        OnboardingAbandoned = _meter.CreateCounter<long>("cobryx_onboarding_abandoned_total", description: "Total tenants that abandoned onboarding");
        TrialExpiredNoWow = _meter.CreateCounter<long>("cobryx_trial_expired_no_wow_total", description: "Total trials that expired without any Wow event");
        SlowActivation = _meter.CreateCounter<long>("cobryx_slow_activation_total", description: "Total tenants that took >24h to reach Wow moment");

        TimeToExpansion = _meter.CreateHistogram<double>("cobryx_time_to_expansion_seconds", unit: "s", description: "Time from first payment to first plan expansion");

        RevenueConcentration = _meter.CreateObservableGauge<double>("cobryx_revenue_concentration_percent",
            () => _revenueConcentrationProvider(), unit: "%", description: "Percentage of total MRR coming from the Top 10% of tenants");
    }

    public void RecordOutcome(string outcomeCode, bool success, string? tier = null, string? httpStatus = null)
    {
        var module = GetModule(outcomeCode);

        // Legacy metric
        if (success)
        {
            BusinessOutcomes.Add(1,
                new KeyValuePair<string, object?>("code", outcomeCode),
                new KeyValuePair<string, object?>("module", module.ToString()));
        }

        // Tier-aware business metric
        CobryxOutcomeTotal.Add(1,
            new KeyValuePair<string, object?>("code", outcomeCode),
            new KeyValuePair<string, object?>("success", success.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("tier", tier ?? "unknown"),
            new KeyValuePair<string, object?>("status", httpStatus ?? "0"));
    }

    public void RecordRecoveryAttempt(int attemptNumber, string outcome, string? reason = null)
    {
        RecoveryAttemptTotal.Add(1,
            new KeyValuePair<string, object?>("attempt", attemptNumber),
            new KeyValuePair<string, object?>("outcome", outcome),
            new KeyValuePair<string, object?>("reason", reason ?? "none"));
    }

    public void RecordRecoveryRevenue(double amount, string currency)
    {
        RecoveryRevenueTotal.Add(amount, new KeyValuePair<string, object?>("currency", currency));
    }

    public void RecordRecoveryDisputeStop(Guid customerId)
    {
        RecoveryDisputeStopTotal.Add(1, new KeyValuePair<string, object?>("customer_id", customerId.ToString()));
    }

    public void RecordError(string errorCode, int? numericCode = null)
    {
        var module = GetModule(errorCode);
        DomainErrors.Add(1,
            new KeyValuePair<string, object?>("code", errorCode),
            new KeyValuePair<string, object?>("module", module.ToString()),
            new KeyValuePair<string, object?>("numeric_code", numericCode));
    }

    public void RecordTimeToWow(double seconds, string outcomeCode)
    {
        TimeToWow.Record(seconds, new KeyValuePair<string, object?>("code", outcomeCode));
    }

    public void RecordFeatureActivation(string featureName)
    {
        FeatureActivation.Add(1, new KeyValuePair<string, object?>("feature", featureName));
    }

    public void RecordOnboardingAbandoned() => OnboardingAbandoned.Add(1);
    public void RecordTrialExpiredNoWow() => TrialExpiredNoWow.Add(1);
    public void RecordSlowActivation() => SlowActivation.Add(1);

    public void RecordTimeToExpansion(double seconds)
    {
        TimeToExpansion.Record(seconds);
    }

    private static CobryxModule GetModule(string code)
    {
        if (string.IsNullOrEmpty(code)) return CobryxModule.System;

        var prefix = code.Split('.')[0].ToUpperInvariant();

        if (Enum.TryParse<CobryxModule>(prefix, true, out var module))
        {
            return module;
        }

        return CobryxModule.Other;
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
