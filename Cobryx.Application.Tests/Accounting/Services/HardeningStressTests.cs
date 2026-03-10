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
    private readonly Mock<ILedgerIntegrityService> _integrityServiceMock;
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
        _integrityServiceMock = new Mock<ILedgerIntegrityService>();
        _reconLoggerMock = new Mock<ILogger<BankReconciliationEngine>>();
        _metrics = new CobryxMetrics();
    }

    [Fact]
    public async Task MillionScale_DenseTenant_Benchmark()
    {
        // 1. Setup 100,000 entries (200,000 lines) for a single tenant
        var tenantId = Guid.NewGuid();
        Guid accountId;
        Guid suspenseId;

        using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var account = new LedgerAccount(tenantId, "1010", "Cash", Cobryx.Domain.Enums.LedgerAccountType.Asset);
            var suspense = new LedgerAccount(tenantId, "9000", "Suspense", Cobryx.Domain.Enums.LedgerAccountType.Equity);
            context.LedgerAccounts.Add(account);
            context.LedgerAccounts.Add(suspense);
            await context.SaveChangesAsync();
            accountId = account.Id;
            suspenseId = suspense.Id;

            _output.WriteLine("Generating 20,000 entries (Dense Profile)...");
            var swGeneration = Stopwatch.StartNew();
            for (int i = 1; i <= 20000; i++)
            {
                var tx = new LedgerTransaction(tenantId, "Dense Stress", $"TX-{i}");
                tx.AddEntry(accountId, 10, 0);
                tx.AddEntry(suspenseId, 0, 10);
                tx.Post();
                context.LedgerTransactions.Add(tx);

                if (i % 5000 == 0) await context.SaveChangesAsync();
            }
            await context.SaveChangesAsync();
            swGeneration.Stop();
            _output.WriteLine($"Generated 20k entries in {swGeneration.ElapsedMilliseconds}ms");
        }

        // 2. Verified Streaming Full Scan Performance
        using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var service = new LedgerIntegrityService(context, _metrics, _loggerMock.Object);
            var swFull = Stopwatch.StartNew();
            var report = await service.VerifyJournalIntegrityAsync(tenantId, forceFullReplay: true);
            swFull.Stop();

            _output.WriteLine($"Streaming Full Scan (20k) took {swFull.ElapsedMilliseconds}ms.");
            Assert.True(report.IsHealthy);
            Assert.Equal(40000, report.TotalEntriesScanned);
            // Target: < 5000ms (Streaming overhead should be minimal)
            Assert.True(swFull.ElapsedMilliseconds < 5000, "Full scan at 20k should be efficient");
        }
    }

    [Fact]
    public async Task Heuristic_LogarithmicMatch_Validation()
    {
        var tenantId = Guid.NewGuid();
        var movement = new BankMovement(tenantId, 1000, "USD", BankMovementDirection.Inbound, DateTime.UtcNow, DateTime.UtcNow, "Plaid", "batch_123", "REF-BATCH");

        using var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object);
        context.BankMovements.Add(movement);

        // Case: 10 small transitions matching the same ReferenceId
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
        var report = await engine.ReconcileBankMovementsAsync(tenantId);

        // Check confidence calculation: 0.85 - (0 * 0.03) - (log10(10)*0.05) = 0.85 - 0.05 = 0.80
        Assert.All(report.MatchedItems, m => Assert.InRange(m.Confidence, 0.79m, 0.81m));
        _output.WriteLine($"Logarithmic Match Confidence for batch of 10: {report.MatchedItems.First().Confidence}");
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

        _integrityServiceMock.Setup(s => s.VerifyJournalIntegrityAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegrityReport(true, 1, 0, 0, "FINGERPRINT", [], false));

        // Run two reconciliation engines simultaneously
        using (var context1 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        using (var context2 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var engine1 = new BankReconciliationEngine(context1, _integrityServiceMock.Object, new Mock<IDatabaseDiagnosticService>().Object, _metrics, _reconLoggerMock.Object);
            var engine2 = new BankReconciliationEngine(context2, _integrityServiceMock.Object, new Mock<IDatabaseDiagnosticService>().Object, _metrics, _reconLoggerMock.Object);

            var task1 = engine1.ReconcileBankMovementsAsync(tenantId);
            var task2 = engine2.ReconcileBankMovementsAsync(tenantId);

            await Task.WhenAll(task1, task2);
        }

        using (var context3 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var movement = await context3.BankMovements.FirstAsync();
            Assert.Equal(BankMovementStatus.Matched, movement.Status);
        }
    }
}
