using Microsoft.Extensions.Logging;

namespace Cobryx.Application.ML.Jobs;

public class DatasetExporterJob
{
    private readonly DatasetExporter _exporter;
    private readonly ILogger<DatasetExporterJob> _logger;

    public DatasetExporterJob(DatasetExporter exporter, ILogger<DatasetExporterJob> logger)
    {
        _exporter = exporter;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Starting ML DatasetExporterJob");

        var path = System.Environment.GetEnvironmentVariable("ML_DATASET_PATH") ?? "ml-service/data/dataset.csv";

        await _exporter.ExportAsync(path);

        _logger.LogInformation("Finished ML DatasetExporterJob successfully");
    }
}
