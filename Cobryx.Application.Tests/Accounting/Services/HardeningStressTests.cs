using System.Diagnostics;

using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Xunit.Abstractions;

namespace Cobryx.Application.Tests.Accounting.Services;

public class HardeningStressTests(ITestOutputHelper output)
{
    private readonly DbContextOptions<CobryxDbContext> _dbOptions = new DbContextOptionsBuilder<CobryxDbContext>()
            .UseInMemoryDatabase(databaseName: $"StressDb_{Guid.NewGuid()}")
            .Options;
    private readonly Mock<ITenantProvider> _tenantProviderMock = new();
    private readonly Mock<ILogger<LedgerIntegrityService>> _loggerMock = new();
    private readonly Mock<ILedgerIntegrityService> _integrityServiceMock = new();
    private readonly Mock<ILogger<BankReconciliationEngine>> _reconLoggerMock = new();
    private readonly CobryxMetrics _metrics = new();


    [Fact]
    public async Task MillionScale_DenseTenant_Benchmark()
    {
        var tenantId = Guid.NewGuid();
        Guid accountId;
        Guid suspenseId;

        await using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var account = new LedgerAccount(tenantId, "1010", "Cash", LedgerAccountType.Asset);
            var suspense = new LedgerAccount(tenantId, "9000", "Suspense", LedgerAccountType.Equity);
            context.LedgerAccounts.Add(account);
            context.LedgerAccounts.Add(suspense);
            await context.SaveChangesAsync();
            accountId = account.Id;
            suspenseId = suspense.Id;

            output.WriteLine("Generating 20,000 entries (Dense Profile)...");
            var swGeneration = Stopwatch.StartNew();
            for (var i = 1; i <= 20000; i++)
            {
                var tx = new LedgerTransaction(tenantId, "Dense Stress", $"TX-{i}");
                tx.AddEntry(accountId, 10, 0);
                tx.AddEntry(suspenseId, 0, 10);
                tx.Post();
                context.LedgerTransactions.Add(tx);

                if (i % 5000 == 0)
                    await context.SaveChangesAsync();
            }
            await context.SaveChangesAsync();
            swGeneration.Stop();
            output.WriteLine($"Generated 20k entries in {swGeneration.ElapsedMilliseconds}ms");
        }

        await using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var service = new LedgerIntegrityService(context, _metrics, _loggerMock.Object);
            var swFull = Stopwatch.StartNew();
            IntegrityReport report = await service.VerifyJournalIntegrityAsync(tenantId, forceFullReplay: true);
            swFull.Stop();

            output.WriteLine($"Streaming Full Scan (20k) took {swFull.ElapsedMilliseconds}ms.");
            Assert.True(report.IsHealthy);
            Assert.Equal(40000, report.TotalEntriesScanned);
            Assert.True(swFull.ElapsedMilliseconds < 5000, "Full scan at 20k should be efficient");
        }
    }

    [Fact]
    public async Task Heuristic_LogarithmicMatch_Validation()
    {
        var tenantId = Guid.NewGuid();
        var movement = new BankMovement(tenantId, 1000, "USD", BankMovementDirection.Inbound, DateTime.UtcNow, DateTime.UtcNow, "Plaid", "batch_123", "REF-BATCH");

        await using var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object);
        context.BankMovements.Add(movement);

        for (int i = 1; i <= 10; i++)
        {
            var tx = new LedgerTransaction(tenantId, $"Item {i}", "REF-BATCH");
            tx.AddEntry(Guid.NewGuid(), 100, 0);
            tx.AddEntry(Guid.NewGuid(), 0, 100);
            tx.Post();
            context.LedgerTransactions.Add(tx);
        }
        await context.SaveChangesAsync();

        _integrityServiceMock.Setup(s => s.VerifyJournalIntegrityAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegrityReport(true, 1, 0, 0, "FINGERPRINT", [], false));

        var engine = new BankReconciliationEngine(context, _integrityServiceMock.Object, new Mock<IDatabaseDiagnosticService>().Object, _metrics, _reconLoggerMock.Object);
        BankReconciliationReport report = await engine.ReconcileBankMovementsAsync(tenantId);

        Assert.All(report.MatchedItems, m => Assert.InRange(m.Confidence, 0.79m, 0.81m));
        output.WriteLine($"Logarithmic Match Confidence for batch of 10: {report.MatchedItems.First().Confidence}");
    }

    [Fact]
    public async Task ConcurrencyTest_SimultaneousReconciliation_ShouldBeSafe()
    {
        var tenantId = Guid.NewGuid();

        await using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
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

        _integrityServiceMock.Setup(s => s.VerifyJournalIntegrityAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegrityReport(true, 1, 0, 0, "FINGERPRINT", [], false));

        await using (var context1 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        await using (var context2 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var engine1 = new BankReconciliationEngine(context1, _integrityServiceMock.Object, new Mock<IDatabaseDiagnosticService>().Object, _metrics, _reconLoggerMock.Object);
            var engine2 = new BankReconciliationEngine(context2, _integrityServiceMock.Object, new Mock<IDatabaseDiagnosticService>().Object, _metrics, _reconLoggerMock.Object);

            Task<BankReconciliationReport> task1 = engine1.ReconcileBankMovementsAsync(tenantId);
            Task<BankReconciliationReport> task2 = engine2.ReconcileBankMovementsAsync(tenantId);

            await Task.WhenAll(task1, task2);
        }

        await using (var context3 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var movement = await context3.BankMovements.FirstAsync();
            Assert.Equal(BankMovementStatus.Matched, movement.Status);
        }
    }
}
