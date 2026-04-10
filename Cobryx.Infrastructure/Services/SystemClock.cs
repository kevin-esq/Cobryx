using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Services;

/// <summary>
/// Production implementation of IClock with clock skew detection.
/// For critical financial operations, validates against DB time periodically.
/// </summary>
public partial class SystemClock : IClock
{
    private readonly ILogger<SystemClock>? _logger;
    private readonly CobryxMetrics? _metrics;

    private static DateTime _lastDbTime = DateTime.MinValue;
    private static DateTime _lastLocalTime = DateTime.MinValue;
    private static readonly object _syncLock = new();
    private static TimeSpan _estimatedSkew = TimeSpan.Zero;

    private const int SkewThresholdSeconds = 5;
    private const int DbSyncIntervalMinutes = 5;

    public SystemClock()
    {
    }

    public SystemClock(ILogger<SystemClock> logger, CobryxMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public DateTime UtcNow
    {
        get
        {
            var localNow = DateTime.UtcNow;

            // Apply estimated skew correction if significant
            if (Math.Abs(_estimatedSkew.TotalSeconds) > SkewThresholdSeconds)
            {
                return localNow.Add(_estimatedSkew);
            }

            return localNow;
        }
    }

    /// <summary>
    /// Calibrate clock against database time. Call periodically from a background job.
    /// </summary>
    public void CalibrateFromDbTime(DateTime dbTime)
    {
        lock (_syncLock)
        {
            var localNow = DateTime.UtcNow;
            var skew = dbTime - localNow;

            if (Math.Abs(skew.TotalSeconds) > SkewThresholdSeconds)
            {
                _estimatedSkew = skew;
                _metrics?.ClockSkewDetectedTotal.Add(1);

                if (_logger != null)
                {
                    LogClockSkewDetected(_logger, skew.TotalSeconds);
                }
            }
            else
            {
                _estimatedSkew = TimeSpan.Zero;
            }

            _lastDbTime = dbTime;
            _lastLocalTime = localNow;
        }
    }

    /// <summary>
    /// Check if clock needs recalibration.
    /// </summary>
    public bool NeedsCalibration()
    {
        if (_lastDbTime == DateTime.MinValue)
            return true;

        return (DateTime.UtcNow - _lastLocalTime).TotalMinutes > DbSyncIntervalMinutes;
    }

    /// <summary>
    /// Get current estimated skew for monitoring.
    /// </summary>
    public TimeSpan GetEstimatedSkew() => _estimatedSkew;

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Clock skew detected: {SkewSeconds}s difference from DB time")]
    private static partial void LogClockSkewDetected(ILogger logger, double skewSeconds);
}
