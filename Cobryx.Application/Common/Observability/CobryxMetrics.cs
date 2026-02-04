using System.Diagnostics.Metrics;

namespace Cobryx.Application.Common.Observability;

public sealed class CobryxMetrics : IDisposable
{
    public const string MeterName = "Cobryx.Api";

    private readonly Meter _meter;

    public Counter<long> LoginSuccesses { get; }
    public Counter<long> LoginFailures { get; }
    public Counter<long> TokenRefreshes { get; }
    public Counter<long> PasswordResets { get; }
    public Counter<long> OutboxJobsProcessed { get; }

    public CobryxMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        LoginSuccesses = _meter.CreateCounter<long>("auth_login_success_total", description: "Total number of successful logins");
        LoginFailures = _meter.CreateCounter<long>("auth_login_failure_total", description: "Total number of failed logins");
        TokenRefreshes = _meter.CreateCounter<long>("auth_token_refresh_total", description: "Total number of token refresh operations");
        PasswordResets = _meter.CreateCounter<long>("auth_password_reset_total", description: "Total number of password resets requested");
        OutboxJobsProcessed = _meter.CreateCounter<long>("outbox_jobs_processed_total", description: "Total number of outbox jobs processed");
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
