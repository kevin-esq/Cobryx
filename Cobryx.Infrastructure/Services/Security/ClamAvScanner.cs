using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nClam;
using Cobryx.Domain.Exceptions.System;

namespace Cobryx.Infrastructure.Services.Security;

public class ClamAvScanner : IVirusScanner
{
    private readonly string _host;
    private readonly int _port;
    private readonly ILogger<ClamAvScanner> _logger;

    public ClamAvScanner(IOptions<ClamAvOptions> options, ILogger<ClamAvScanner> logger)
    {
        _host = options.Value.Host;
        _port = options.Value.Port;
        _logger = logger;
    }

    public async Task<bool> IsSafeAsync(Stream file)
    {
        try
        {
            var clam = new ClamClient(_host, _port);
            var scanResult = await clam.SendAndScanFileAsync(file);

            switch (scanResult.Result)
            {
                case ClamScanResults.Clean:
                    return true;
                case ClamScanResults.VirusDetected:
                    _logger.LogWarning("Virus detected in file!");
                    return false;
                case ClamScanResults.Error:
                    _logger.LogError("Error scanning file for viruses.");
                    throw new SystemConfigurationException();
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to ClamAV server at {Host}:{Port}", _host, _port);
            throw;
        }
    }
}
