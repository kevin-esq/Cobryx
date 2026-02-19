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
    public Counter<long> DomainErrors { get; }
    public Counter<long> LoginSuccesses { get; }
    public Counter<long> LoginFailures { get; }
    public Counter<long> TokenRefreshes { get; }
    public Counter<long> PasswordResets { get; }
    public Counter<long> OutboxJobsProcessed { get; }

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

    public CobryxMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        BusinessOutcomes = _meter.CreateCounter<long>(
            "cobryx_business_outcomes_total",
            description: "Total number of successful business outcomes");

        DomainErrors = _meter.CreateCounter<long>(
            "cobryx_domain_errors_total",
            description: "Total number of domain errors");

        LoginSuccesses = _meter.CreateCounter<long>("auth_login_success_total", description: "Total number of successful logins");
        LoginFailures = _meter.CreateCounter<long>("auth_login_failure_total", description: "Total number of failed logins");
        TokenRefreshes = _meter.CreateCounter<long>("auth_token_refresh_total", description: "Total number of token refresh operations");
        PasswordResets = _meter.CreateCounter<long>("auth_password_reset_total", description: "Total number of password resets requested");
        OutboxJobsProcessed = _meter.CreateCounter<long>("outbox_jobs_processed_total", description: "Total number of outbox jobs processed");

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
    }

    public void RecordOutcome(string outcomeCode)
    {
        var module = GetModule(outcomeCode);
        BusinessOutcomes.Add(1,
            new KeyValuePair<string, object?>("code", outcomeCode),
            new KeyValuePair<string, object?>("module", module.ToString()));
    }

    public void RecordError(string errorCode, int? numericCode = null)
    {
        var module = GetModule(errorCode);
        DomainErrors.Add(1,
            new KeyValuePair<string, object?>("code", errorCode),
            new KeyValuePair<string, object?>("module", module.ToString()),
            new KeyValuePair<string, object?>("numeric_code", numericCode));
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
