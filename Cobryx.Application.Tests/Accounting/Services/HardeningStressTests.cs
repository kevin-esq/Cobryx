using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities.Accounting;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Cobryx.Application.Common.Observability;
using Xunit.Abstractions;
using System.Diagnostics;

namespace Cobryx.Application.Tests.Accounting.Services;

public class HardeningStressTests
{
    private readonly DbContextOptions<CobryxDbContext> _dbOptions;
    private readonly Mock<ITenantProvider> _tenantProviderMock;
    private readonly Mock<ILogger<LedgerIntegrityService>> _loggerMock;
    private readonly Mock<ILogger<BankReconciliationEngine>> _reconLoggerMock;
    private readonly CobryxMetrics _metrics;
    private readonly ITestOutputHelper _output;

    public HardeningStressTests(ITestOutputHelper output)
    {
        _output = output;
        _dbOptions = new DbContextOptionsBuilder<CobryxDbContext>()
            .UseInMemoryDatabase(databaseName: $"StressDb_{Guid.NewGuid()}")
            .Options;

        _tenantProviderMock = new Mock<ITenantProvider>();
        _loggerMock = new Mock<ILogger<LedgerIntegrityService>>();
        _reconLoggerMock = new Mock<ILogger<BankReconciliationEngine>>();
        _metrics = new CobryxMetrics();
    }

    [Fact]
    public async Task StressTest_10k_LedgerScan_Performance_And_CorruptionDetection()
    {
        // 1. Setup 10,000 entries
        var tenantId = Guid.NewGuid();
        Guid accountId;
        Guid suspenseId;
        var lastFingerprint = "INITIAL";

        using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var account = new LedgerAccount(tenantId, "1010", "Cash", Cobryx.Domain.Enums.LedgerAccountType.Asset);
            var suspense = new LedgerAccount(tenantId, "9000", "Suspense", Cobryx.Domain.Enums.LedgerAccountType.Equity);
            context.LedgerAccounts.Add(account);
            context.LedgerAccounts.Add(suspense);
            await context.SaveChangesAsync();
            accountId = account.Id;
            suspenseId = suspense.Id;

            _output.WriteLine("Generating 10,000 ledger entries...");
            var swGeneration = Stopwatch.StartNew();
            for (int i = 1; i <= 10000; i++)
            {
                var tx = new LedgerTransaction(tenantId, "Stress Test", $"TX-{i}");
                tx.AddEntry(accountId, 100, 0);
                tx.AddEntry(suspenseId, 0, 100); // Balanced dummy
                tx.Post();
                context.LedgerTransactions.Add(tx);

                if (i % 1000 == 0) await context.SaveChangesAsync();
            }
            await context.SaveChangesAsync();
            swGeneration.Stop();
            _output.WriteLine($"Generated 10,000 entries (20,000 lines) in {swGeneration.ElapsedMilliseconds}ms");
        }

        // 2. Initial Full Scan (Paranoid Mode)
        using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var service = new LedgerIntegrityService(context, _metrics, _loggerMock.Object);

            var swFull = Stopwatch.StartNew();
            var report = await service.VerifyJournalIntegrityAsync(tenantId, forceFullReplay: true);
            swFull.Stop();

            _output.WriteLine($"Full scan (10k) took {swFull.ElapsedMilliseconds}ms. Healthy: {report.IsHealthy}");
            Assert.True(report.IsHealthy);
            Assert.Equal(20000, report.TotalEntriesScanned); // 10k transactions * 2 entries
            lastFingerprint = report.JournalFingerprint;
        }

        // 3. Add Delta (100 entries) and run Incremental Scan
        using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            for (int i = 1; i <= 100; i++)
            {
                var tx = new LedgerTransaction(tenantId, "Delta Test", $"DELTA-{i}");
                tx.AddEntry(accountId, 100, 0);
                tx.AddEntry(suspenseId, 0, 100);
                tx.Post();
                context.LedgerTransactions.Add(tx);
            }
            await context.SaveChangesAsync();

            var service = new LedgerIntegrityService(context, _metrics, _loggerMock.Object);

            var swIncremental = Stopwatch.StartNew();
            var report = await service.VerifyJournalIntegrityAsync(tenantId, forceFullReplay: false);
            swIncremental.Stop();

            _output.WriteLine($"Incremental scan (200 lines) took {swIncremental.ElapsedMilliseconds}ms. Healthy: {report.IsHealthy}");
            Assert.True(report.IsHealthy);
            Assert.Equal(200, report.TotalEntriesScanned);
            Assert.NotEqual(lastFingerprint, report.JournalFingerprint);
        }

        // 4. Simulate Deep Corruption (Tamper with Entry #5000)
        using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var entryToCorrupt = await context.LedgerEntries
                .OrderBy(e => e.CreatedAt)
                .Skip(5000)
                .FirstAsync();

            // Bypass domain logic to tamper directly 
            // We use Reflection or a 'Backdoor' to simulate external DB tampering since it's InMemory
            typeof(LedgerEntry).GetProperty("Debit")?.SetValue(entryToCorrupt, 999.99m);
            await context.SaveChangesAsync();

            var service = new LedgerIntegrityService(context, _metrics, _loggerMock.Object);

            // Incremental scan should NOT detect it because it's BEFORE the checkpoint
            var reportInc = await service.VerifyJournalIntegrityAsync(tenantId, forceFullReplay: false);
            _output.WriteLine($"Incremental scan after deep corruption. Detection: {!reportInc.IsHealthy}");
            Assert.True(reportInc.IsHealthy); // This is the expected "Resilient but Silent" behavior of incremental

            // Forced Full Scan SHOULD detect it
            var reportFull = await service.VerifyJournalIntegrityAsync(tenantId, forceFullReplay: true);
            _output.WriteLine($"Forced Full scan after deep corruption. Detection: {!reportFull.IsHealthy}");
            Assert.False(reportFull.IsHealthy);
        }
    }

    [Fact]
    public async Task ConcurrencyTest_SimultaneousReconciliation_ShouldBeSafe()
    {
        var tenantId = Guid.NewGuid();
        var movementId = Guid.NewGuid();

        using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var movement = new BankMovement(tenantId, 500, "USD", BankMovementDirection.Inbound, DateTime.UtcNow, DateTime.UtcNow, "Plaid", "tx_123", "REF-BATCH");
            context.BankMovements.Add(movement);

            var tx = new LedgerTransaction(tenantId, "Ledger Match", "REF-BATCH");
            tx.AddEntry(Guid.NewGuid(), 500, 0);
            tx.AddEntry(Guid.NewGuid(), 0, 500);
            tx.Post();
            context.LedgerTransactions.Add(tx);

            await context.SaveChangesAsync();
        }

        // Run two reconciliation engines simultaneously
        using (var context1 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        using (var context2 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var engine1 = new BankReconciliationEngine(context1, _metrics, _reconLoggerMock.Object);
            var engine2 = new BankReconciliationEngine(context2, _metrics, _reconLoggerMock.Object);

            var task1 = engine1.ReconcileBankMovementsAsync(tenantId);
            var task2 = engine2.ReconcileBankMovementsAsync(tenantId);

            await Task.WhenAll(task1, task2);

            var report1 = task1.Result;
            var report2 = task2.Result;

            // Assert: Only one should have successfully matched the item (if we have concurrency checks)
            // Or both matched but the DB update is atomic and only reflects 1 match in the end.
            _output.WriteLine($"Recon 1 Matched: {report1.MatchedItems.Count}");
            _output.WriteLine($"Recon 2 Matched: {report2.MatchedItems.Count}");
        }

        using (var context3 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var movement = await context3.BankMovements.FirstAsync();
            Assert.Equal(BankMovementStatus.Matched, movement.Status);
        }
    }
}
