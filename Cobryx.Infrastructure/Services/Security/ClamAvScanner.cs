using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Exceptions.System;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using nClam;

namespace Cobryx.Infrastructure.Services.Security;

public class ClamAvScanner : IVirusScanner
{
    private readonly string _host;
    private readonly int _port;
    private readonly ILogger<ClamAvScanner> _logger;
    private readonly ScannerCircuitBreaker _circuitBreaker;
    private readonly CobryxMetrics _metrics;

    public ClamAvScanner(
        IOptions<ClamAvOptions> options,
        ILogger<ClamAvScanner> logger,
        ScannerCircuitBreaker circuitBreaker,
        CobryxMetrics metrics)
    {
        _host = options.Value.Host;
        _port = options.Value.Port;
        _logger = logger;
        _circuitBreaker = circuitBreaker;
        _metrics = metrics;
    }

    public async Task<bool> IsSafeAsync(Stream file)
    {
        if (!_circuitBreaker.AllowRequest())
        {
            _metrics.ScannerCircuitBreakerTrips.Add(1);
            _logger.LogWarning("Scanner circuit breaker is open — rejecting scan request");
            throw new ScannerUnavailableException();
        }

        try
        {
            var clam = new ClamClient(_host, _port);
            var scanResult = await clam.SendAndScanFileAsync(file);

            switch (scanResult.Result)
            {
                case ClamScanResults.Clean:
                    _circuitBreaker.RecordSuccess();
                    return true;
                case ClamScanResults.VirusDetected:
                    _circuitBreaker.RecordSuccess(); // Scanner worked — virus is valid result
                    _logger.LogWarning("Virus detected in file!");
                    return false;
                case ClamScanResults.Error:
                    _circuitBreaker.RecordFailure();
                    _logger.LogError("Error scanning file for viruses.");
                    throw new SystemConfigurationException();
                default:
                    _circuitBreaker.RecordSuccess();
                    return false;
            }
        }
        catch (ScannerUnavailableException)
        {
            throw; // Don't double-record
        }
        catch (Exception ex)
        {
            _circuitBreaker.RecordFailure();
            _logger.LogError(ex, "Failed to connect to ClamAV server at {Host}:{Port}", _host, _port);
            throw;
        }
    }
}
