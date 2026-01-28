using Cobryx.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using nClam;

namespace Cobryx.Infrastructure.Services.Security;

public class ClamAvScanner : IVirusScanner
{
    private readonly string _host;
    private readonly int _port;
    private readonly ILogger<ClamAvScanner> _logger;

    public ClamAvScanner(IConfiguration configuration, ILogger<ClamAvScanner> logger)
    {
        _host = configuration["Security:ClamAV:Host"] ?? "localhost";
        _port = int.Parse(configuration["Security:ClamAV:Port"] ?? "3310");
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
                    throw new InvalidOperationException("Could not complete virus scan.");
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
