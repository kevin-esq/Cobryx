using Cobryx.Application.Common.Observability;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Payments.Stripe;

/// <summary>
/// Resilience policies for Stripe API calls.
/// Prevents retry storms and protects against external API saturation.
///
/// Policies applied:
/// 1. Rate Limit: Max 50 requests/second (Stripe's limit is 100/s)
/// 2. Bulkhead: Max 10 concurrent requests
/// 3. Circuit Breaker: Opens after 5 failures in 30s
/// 4. Retry: 3 retries with exponential backoff
/// 5. Timeout: 30s per request
/// </summary>
public partial class StripeResiliencePolicy
{
    private readonly ILogger<StripeResiliencePolicy> _logger;
    private readonly CobryxMetrics _metrics;

    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly SemaphoreSlim _rateLimiter;

    private static int _failureCount;
    private static DateTime _circuitOpenedAt = DateTime.MinValue;
    private static readonly object _circuitLock = new();

    private const int MaxRequestsPerSecond = 50;
    private const int MaxConcurrentRequests = 10;
    private const int CircuitBreakerFailureThreshold = 5;
    private const int CircuitBreakerDurationSeconds = 30;
    private const int MaxRetries = 3;
    private const int TimeoutSeconds = 30;

    public StripeResiliencePolicy(
        ILogger<StripeResiliencePolicy> logger,
        CobryxMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;

        _concurrencyLimiter = new SemaphoreSlim(MaxConcurrentRequests, MaxConcurrentRequests);
        _rateLimiter = new SemaphoreSlim(MaxRequestsPerSecond, MaxRequestsPerSecond);

        // Replenish rate limiter every second
        _ = ReplenishRateLimiterAsync();
    }

    private async Task ReplenishRateLimiterAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var toRelease = MaxRequestsPerSecond - _rateLimiter.CurrentCount;
            if (toRelease > 0)
            {
                _rateLimiter.Release(toRelease);
            }
        }
    }

    /// <summary>
    /// Execute a Stripe API call with full resilience protection.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        // 1. Check circuit breaker
        if (IsCircuitOpen())
        {
            LogCircuitOpen(_logger);
            _metrics.StripeCircuitBreakerOpenTotal.Add(1);
            throw new InvalidOperationException("Stripe circuit breaker is open");
        }

        // 2. Rate limiting
        if (!await _rateLimiter.WaitAsync(TimeSpan.FromSeconds(5), ct))
        {
            LogRateLimited(_logger);
            _metrics.StripeRateLimitedTotal.Add(1);
            throw new InvalidOperationException("Stripe rate limit exceeded");
        }

        // 3. Concurrency limiting (bulkhead)
        await _concurrencyLimiter.WaitAsync(ct);
        try
        {
            // 4. Execute with timeout and retry
            return await ExecuteWithRetryAsync(action, ct);
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

                var result = await action(cts.Token);

                // Success - reset failure count
                ResetCircuitBreaker();
                return result;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Timeout
                LogTimeout(_logger, TimeoutSeconds);
                _metrics.StripeTimeoutTotal.Add(1);
                RecordFailure();
                lastException = new TimeoutException($"Stripe API timeout after {TimeoutSeconds}s");
            }
            catch (global::Stripe.StripeException ex) when (IsRetryable(ex))
            {
                LogRetry(_logger, attempt + 1, ex.Message);
                _metrics.StripeRetryTotal.Add(1);
                RecordFailure();
                lastException = ex;

                if (attempt < MaxRetries)
                {
                    // Exponential backoff: 500ms, 1s, 2s
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt)), ct);
                }
            }
            catch
            {
                // Non-retryable error
                RecordFailure();
                throw;
            }
        }

        throw lastException ?? new InvalidOperationException("Stripe API call failed");
    }

    private static bool IsRetryable(global::Stripe.StripeException ex)
    {
        return ex.StripeError?.Type == "api_error" ||
               ex.HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests ||
               ex.HttpStatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
               ex.HttpStatusCode == System.Net.HttpStatusCode.BadGateway ||
               ex.HttpStatusCode == System.Net.HttpStatusCode.GatewayTimeout;
    }

    private void RecordFailure()
    {
        lock (_circuitLock)
        {
            _failureCount++;
            if (_failureCount >= CircuitBreakerFailureThreshold)
            {
                _circuitOpenedAt = DateTime.UtcNow;
                LogCircuitOpened(_logger);
            }
        }
    }

    private void ResetCircuitBreaker()
    {
        lock (_circuitLock)
        {
            if (_failureCount > 0)
            {
                _failureCount = 0;
                if (_circuitOpenedAt != DateTime.MinValue)
                {
                    _circuitOpenedAt = DateTime.MinValue;
                    LogCircuitClosed(_logger);
                }
            }
        }
    }

    private static bool IsCircuitOpen()
    {
        lock (_circuitLock)
        {
            if (_circuitOpenedAt == DateTime.MinValue)
                return false;

            // Check if circuit should close (after duration)
            if ((DateTime.UtcNow - _circuitOpenedAt).TotalSeconds > CircuitBreakerDurationSeconds)
            {
                _circuitOpenedAt = DateTime.MinValue;
                _failureCount = 0;
                return false;
            }

            return true;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stripe API timeout after {TimeoutSeconds}s")]
    private static partial void LogTimeout(ILogger logger, int timeoutSeconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stripe API retry attempt {Attempt}: {Reason}")]
    private static partial void LogRetry(ILogger logger, int attempt, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Stripe circuit breaker OPENED - API calls will fail fast")]
    private static partial void LogCircuitOpened(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe circuit breaker CLOSED - resuming normal operations")]
    private static partial void LogCircuitClosed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stripe API rate limited - request queued or rejected")]
    private static partial void LogRateLimited(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stripe circuit breaker is OPEN - failing fast")]
    private static partial void LogCircuitOpen(ILogger logger);
}
