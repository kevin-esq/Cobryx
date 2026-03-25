using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision;
using Cobryx.Domain.Decision;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs.Decision
{
    /// <summary>
    /// Background worker that consumes decision snapshots from the SnapshotStore channel
    /// and persists them to the database in batches.
    /// </summary>
    public class SnapshotBackgroundWorker(
        SnapshotStore store,
        IServiceScopeFactory scopeFactory,
        ILogger<SnapshotBackgroundWorker> logger) : BackgroundService
    {
        private const int BatchSize = 100;
        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(5);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Snapshot Background Worker started.");

            var batch = new List<ProductionSnapshot>(BatchSize);
            var nextFlush = DateTime.UtcNow + FlushInterval;

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        if (await store.Reader.WaitToReadAsync(stoppingToken))
                        {
                            while (batch.Count < BatchSize && store.Reader.TryRead(out var snapshot))
                            {
                                batch.Add(snapshot);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (batch.Count > 0 && (batch.Count >= BatchSize || DateTime.UtcNow >= nextFlush))
                    {
                        await PersistBatchAsync(batch, stoppingToken);
                        batch.Clear();
                        nextFlush = DateTime.UtcNow + FlushInterval;
                    }

                    if (batch.Count == 0)
                    {
                        await Task.Delay(500, stoppingToken);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogCritical(ex, "FATAL: Snapshot Background Worker encountered an unhandled exception.");
            }
            finally
            {
                if (batch.Count > 0)
                {
                    await PersistBatchAsync(batch, CancellationToken.None);
                }

                logger.LogInformation("Snapshot Background Worker stopped.");
            }
        }

        private async Task PersistBatchAsync(List<ProductionSnapshot> batch, CancellationToken ct)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ICobryxDbContext>();

            try
            {
                context.ProductionSnapshots.AddRange(batch);
                _ = await context.SaveChangesAsync(ct);
                logger.LogDebug("Persisted batch of {Count} decision snapshots.", batch.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist batch of {Count} decision snapshots. Data may be lost.",
                    batch.Count);
            }
        }
    }
}
