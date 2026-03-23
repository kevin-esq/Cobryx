using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Identity;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

namespace Cobryx.Application.Tests.Accounting.Services;

public class LedgerIntegrityTests
{
    private readonly DbContextOptions<CobryxDbContext> _dbOptions;
    private readonly Mock<ILogger<LedgerIntegrityService>> _loggerMock;
    private readonly Mock<ITenantProvider> _tenantProviderMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public LedgerIntegrityTests()
    {
        _loggerMock = new Mock<ILogger<LedgerIntegrityService>>();

        _dbOptions = new DbContextOptionsBuilder<CobryxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock.Setup(x => x.GetTenantId()).Returns(_tenantId);

        using var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object);
        var tenant = new Tenant("Test Tenant", "USD");
        typeof(Tenant).GetProperty("Id")!.SetValue(tenant, _tenantId);
        context.Tenants.Add(tenant);
        context.SaveChanges();
    }

    private LedgerIntegrityService CreateService(CobryxDbContext context)
        => new(context, new CobryxMetrics(), _loggerMock.Object);

    [Fact]
    public async Task VerifyJournalIntegrityAsync_ShouldBeHealthy_WhenLedgerIsCorrect()
    {
        using var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object);
        var acc = new LedgerAccount(_tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available, "USD", true);
        context.LedgerAccounts.Add(acc);

        var tx = new LedgerTransaction(_tenantId, "Valid Tx", "REF-1");
        tx.AddEntry(acc.Id, 100, 0);
        tx.AddEntry(acc.Id, 0, 100);
        context.LedgerTransactions.Add(tx);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var report = await service.VerifyJournalIntegrityAsync(_tenantId);

        Assert.True(report.IsHealthy);
        Assert.Equal(2, report.TotalEntriesScanned);
        Assert.Equal(0, report.ImbalancedTransactionsCount);
        Assert.NotEmpty(report.JournalFingerprint);
    }

    [Fact]
    public async Task VerifyJournalIntegrityAsync_ShouldDetectImbalance_AndTripCircuitBreaker()
    {
        using var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object);
        var acc = new LedgerAccount(_tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available, "USD", true);
        context.LedgerAccounts.Add(acc);

        var tx = new LedgerTransaction(_tenantId, "Imbalanced Tx", "REF-2");
        tx.AddEntry(acc.Id, 110, 0); // 110 Debit
        tx.AddEntry(acc.Id, 0, 100);  // 100 Credit -> 10 imbalance
        context.LedgerTransactions.Add(tx);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var report = await service.VerifyJournalIntegrityAsync(_tenantId);

        Assert.False(report.IsHealthy);
        Assert.Equal(1, report.ImbalancedTransactionsCount);
        Assert.True(report.CircuitBreakerTripped);

        // Refresh context for verification
        using var contextVerify = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object);
        var tenant = await contextVerify.Tenants.FindAsync(_tenantId);
        Assert.True(tenant?.FinancialSafeMode);
    }

    [Fact]
    public async Task VerifyJournalIntegrityAsync_ShouldDetectHistoricalAlteration_ViaFingerprint()
    {
        using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var acc = new LedgerAccount(_tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available, "USD", true);
            context.LedgerAccounts.Add(acc);

            var tx = new LedgerTransaction(_tenantId, "Alteration Test", "REF-3");
            tx.AddEntry(acc.Id, 50, 0);
            tx.AddEntry(acc.Id, 0, 50);
            context.LedgerTransactions.Add(tx);
            await context.SaveChangesAsync();
        }

        string originalFingerprint;
        using (var context1 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var service1 = CreateService(context1);
            var report1 = await service1.VerifyJournalIntegrityAsync(_tenantId);
            originalFingerprint = report1.JournalFingerprint;
        }

        // Simulate illegal database alteration (direct entry modification)
        using (var contextAlter = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var entry = await contextAlter.LedgerEntries.FirstAsync();
            typeof(LedgerEntry).GetProperty("Debit")!.SetValue(entry, 55m);
            await contextAlter.SaveChangesAsync();
        }

        using (var context2 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var service2 = CreateService(context2);
            var report2 = await service2.VerifyJournalIntegrityAsync(_tenantId);

            // Assert: Fingerprint is the SAME as originalHealthy because it used the checkpoint
            Assert.Equal(originalFingerprint, report2.JournalFingerprint);
            Assert.True(report2.IsHealthy); // Healthy from an incremental delta perspective
        }

        using (var context3 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
        {
            var service3 = CreateService(context3);
            var report3 = await service3.VerifyJournalIntegrityAsync(_tenantId, forceFullReplay: true);

            // Assert: Detects the alteration
            Assert.NotEqual(originalFingerprint, report3.JournalFingerprint);
            Assert.False(report3.IsHealthy);
        }
        _loggerMock.VerifyLog(LogLevel.Information, "Integrity Scan completed. Healthy: false*", Times.Never()); // It's still "healthy" balance-wise (if we changed both), but fingerprint changed
    }
}

// Helper for verifying logs
public static class LoggerExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, string message, Times times)
    {
        var cleanedMessage = message.Replace("*", "");
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => (v.ToString() ?? "").Contains(cleanedMessage)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}
