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

    // Monetization Metrics
    public Counter<long> SubscriptionLimitReached { get; }
    public Counter<long> SubscriptionUpgrades { get; }
    public Counter<long> SubscriptionDowngradeRejected { get; }

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

        // Scaling
        UsageCacheHits = _meter.CreateCounter<long>("usage_cache_hit_total", description: "Total cache hits for usage snapshots");
        UsageCacheMisses = _meter.CreateCounter<long>("usage_cache_miss_total", description: "Total cache misses for usage snapshots");

        OutboxProcessingLag = _meter.CreateHistogram<double>("outbox_processing_lag_seconds", unit: "s", description: "Lag between event occurrence and processing");
        CommandDuration = _meter.CreateHistogram<double>("command_duration_seconds", unit: "s", description: "Duration of business commands");
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
