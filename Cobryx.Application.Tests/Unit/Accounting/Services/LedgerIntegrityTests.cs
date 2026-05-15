using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Identity;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Services.Accounting;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;


namespace Cobryx.Application.Tests.Unit.Accounting.Services
{
    public class LedgerIntegrityTests : IDisposable
    {
        private readonly DbContextOptions<CobryxDbContext> _dbOptions;
        private readonly Mock<ILogger<LedgerIntegrityService>> _loggerMock;
        private readonly Mock<ITenantProvider> _tenantProviderMock;
        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly SqliteConnection _connection;

        public LedgerIntegrityTests()
        {
            _loggerMock = new Mock<ILogger<LedgerIntegrityService>>();

            // Use SQLite in-memory for real SQL translation support
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            _dbOptions = new DbContextOptionsBuilder<CobryxDbContext>()
                .UseSqlite(_connection)
                .Options;

            _tenantProviderMock = new Mock<ITenantProvider>();
            _ = _tenantProviderMock.Setup(static x => x.GetTenantId()).Returns(_tenantId);

            using var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object);
            _ = context.Database.EnsureCreated();
            var tenant = new Tenant("Test Tenant", "USD");
            typeof(Tenant).GetProperty("Id")!.SetValue(tenant, _tenantId);
            _ = context.Tenants.Add(tenant);
            _ = context.SaveChanges();
        }

        public void Dispose() => _connection?.Dispose();

        private LedgerIntegrityService CreateService(CobryxDbContext context, ILedgerHealthCache? healthCache = null)
            => new(context, healthCache ?? new Mock<ILedgerHealthCache>().Object, new Mock<ILedgerHasher>().Object, new Mock<IClock>().Object, new Mock<ILedgerAnchorStore>().Object, _loggerMock.Object);

        [Fact]
        public async Task VerifyJournalIntegrityAsyncShouldBeHealthyWhenLedgerIsCorrect()
        {
            using var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object);
            var acc = new LedgerAccount(_tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available, "USD", true);
            _ = context.LedgerAccounts.Add(acc);

            var tx = new LedgerTransaction(_tenantId, "Valid Tx", "REF-1");
            tx.AddEntry(acc.Id, 100, 0);
            tx.AddEntry(acc.Id, 0, 100);
            _ = context.LedgerTransactions.Add(tx);
            _ = await context.SaveChangesAsync();

            var service = CreateService(context);

            var report = await service.VerifyJournalIntegrityAsync(_tenantId);

            Assert.True(report.IsHealthy);
            Assert.Equal(2, report.TotalEntriesScanned);
            Assert.Empty(report.Violations);
            Assert.NotEmpty(report.JournalFingerprint);
        }

        [Fact]
        public async Task VerifyJournalIntegrityAsyncShouldDetectImbalanceAndTripCircuitBreaker()
        {
            using var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object);
            var acc = new LedgerAccount(_tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available, "USD", true);
            _ = context.LedgerAccounts.Add(acc);

            var tx = new LedgerTransaction(_tenantId, "Imbalanced Tx", "REF-2");
            tx.AddEntry(acc.Id, 110, 0);
            tx.AddEntry(acc.Id, 0, 100);
            _ = context.LedgerTransactions.Add(tx);
            _ = await context.SaveChangesAsync();

            var healthCache = new LedgerHealthCache(new MemoryCache(new MemoryCacheOptions()));
            var service = CreateService(context, healthCache);

            var report = await service.VerifyJournalIntegrityAsync(_tenantId);

            Assert.False(report.IsHealthy);
            Assert.NotEmpty(report.Violations);
            Assert.True(report.CircuitBreakerTripped);

            var status = healthCache.Get(_tenantId);
            Assert.True(status.IsSafeMode);
            Assert.Equal("IMBALANCE", status.Reason);
        }

        [Fact]
        public async Task VerifyJournalIntegrityAsyncShouldDetectHistoricalAlterationViaFingerprint()
        {
            using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
            {
                var acc = new LedgerAccount(_tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available, "USD", true);
                _ = context.LedgerAccounts.Add(acc);

                var tx = new LedgerTransaction(_tenantId, "Alteration Test", "REF-3");
                tx.AddEntry(acc.Id, 50, 0);
                tx.AddEntry(acc.Id, 0, 50);
                _ = context.LedgerTransactions.Add(tx);
                _ = await context.SaveChangesAsync();
            }

            string originalFingerprint;
            using (var context1 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
            {
                var service1 = CreateService(context1);
                var report1 = await service1.VerifyJournalIntegrityAsync(_tenantId);
                originalFingerprint = report1.JournalFingerprint;
            }

            using (var contextAlter = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
            {
                var entry = await contextAlter.LedgerEntries.FirstAsync();
                typeof(LedgerEntry).GetProperty("Debit")!.SetValue(entry, 55m);
                _ = await contextAlter.SaveChangesAsync();
            }

            using (var context2 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
            {
                var service2 = CreateService(context2);
                var report2 = await service2.VerifyJournalIntegrityAsync(_tenantId);

                Assert.Equal(originalFingerprint, report2.JournalFingerprint);
                Assert.True(report2.IsHealthy);
            }

            using (var context3 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
            {
                var service3 = CreateService(context3);
                var report3 = await service3.VerifyJournalIntegrityAsync(_tenantId, forceFullReplay: true);

                Assert.NotEqual(originalFingerprint, report3.JournalFingerprint);
                Assert.False(report3.IsHealthy);
            }
            _loggerMock.VerifyLog(LogLevel.Information, "Integrity Scan completed. Healthy: false*", Times.Never());
        }
    }

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
}
