using System.Diagnostics.Metrics;
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
