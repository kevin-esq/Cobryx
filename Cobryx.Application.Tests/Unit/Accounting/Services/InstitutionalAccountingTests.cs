using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Identity;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Services.Accounting;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Cobryx.Application.Tests.Unit.Accounting.Services
{
    public class InstitutionalAccountingTests : IDisposable
    {
        private readonly DbContextOptions<CobryxDbContext> _dbOptions;
        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly Mock<ITenantProvider> _tenantProviderMock;
        private readonly Mock<ILedgerIntegrityService> _integrityServiceMock;
        private readonly Mock<ILogger<BankReconciliationEngine>> _reconLoggerMock;
        private readonly Mock<IClock> _clockMock;
        private readonly CobryxMetrics _metrics;

        public InstitutionalAccountingTests()
        {
            _dbOptions = new DbContextOptionsBuilder<CobryxDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _tenantProviderMock = new Mock<ITenantProvider>();
            _ = _tenantProviderMock.Setup(static x => x.GetTenantId()).Returns(_tenantId);

            using var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object);
            var tenant = new Tenant("Institutional Tenant", "USD");
            typeof(Tenant).GetProperty("Id")!.SetValue(tenant, _tenantId);
            _ = context.Tenants.Add(tenant);
            _ = context.SaveChanges();
            _integrityServiceMock = new Mock<ILedgerIntegrityService>();
            _ = _integrityServiceMock.Setup(static s => s.VerifyJournalIntegrityAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IntegrityReport(true, AccountingDriftSeverity.Low, 0, [], "INITIAL_FINGERPRINT", false, "corr-id", DateTime.UtcNow));

            _reconLoggerMock = new Mock<ILogger<BankReconciliationEngine>>();
            _clockMock = new Mock<IClock>();
            _metrics = new CobryxMetrics();
        }

        [Fact]
        public async Task BankReconciliation_ShouldSupportMultiLevelMatching()
        {
            using var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object);
            var acc = new LedgerAccount(_tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available, "USD", true);
            _ = context.LedgerAccounts.Add(acc);

            var tx1 = new LedgerTransaction(_tenantId, "Stripe Payout", "PAYOUT-001");
            tx1.AddEntry(acc.Id, 1000m, 0);
            tx1.AddEntry(acc.Id, 0, 1000m);
            tx1.Post();
            _ = context.LedgerTransactions.Add(tx1);

            var tx2 = new LedgerTransaction(_tenantId, "Manual Wire", "WIRE-999");
            tx2.AddEntry(acc.Id, 500m, 0);
            tx2.AddEntry(acc.Id, 0, 500m);
            tx2.Post();
            _ = context.LedgerTransactions.Add(tx2);

            var m1 = new BankMovement(_tenantId, 1000m, "USD", BankMovementDirection.Inbound, DateTime.UtcNow, DateTime.UtcNow, "Plaid", "p1", "PAYOUT-001");
            var m2 = new BankMovement(_tenantId, 500m, "USD", BankMovementDirection.Inbound, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(2), "Plaid", "p2", "WRONG-REF");

            context.BankMovements.AddRange(m1, m2);
            _ = await context.SaveChangesAsync();

            var engine = new BankReconciliationEngine(context, _integrityServiceMock.Object, new Mock<IDatabaseDiagnosticService>().Object, _metrics, _clockMock.Object, _reconLoggerMock.Object);

            var report = await engine.ReconcileBankMovementsAsync(_tenantId);

            Assert.Equal(2, report.MatchedItems.Count);

            var match1 = report.MatchedItems.First(x => x.BankMovementId == m1.Id);
            Assert.Equal(1.0m, match1.Confidence);

            var match2 = report.MatchedItems.First(x => x.BankMovementId == m2.Id);
            Assert.Equal(0.9m, match2.Confidence);
        }

        [Fact(Skip = "Query uses Math.Abs() which is not supported in SQLite/InMemory. Works in PostgreSQL production.")]
        public async Task LedgerIntegrity_ShouldSupportIncrementalReplay_AndIgnoreHistoricalCorruption()
        {
            using (var context = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
            {
                var account = new LedgerAccount(_tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available, "USD", true);
                _ = context.LedgerAccounts.Add(account);

                var tx1 = new LedgerTransaction(_tenantId, "Block 1", "B1");
                tx1.AddEntry(account.Id, 100, 100);
                tx1.Post();

                typeof(LedgerEntry).GetProperty("JournalSequenceId")!.SetValue(tx1.Entries.First(), 1L);

                _ = context.LedgerTransactions.Add(tx1);
                _ = await context.SaveChangesAsync();
            }

            string fingerprint1;
            using (var context1 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
            {
                var service = new LedgerIntegrityService(context1, new Mock<ILedgerHealthCache>().Object, new Mock<ILedgerHasher>().Object, _clockMock.Object, new Mock<ILedgerAnchorStore>().Object, Mock.Of<ILogger<LedgerIntegrityService>>());
                var report = await service.VerifyJournalIntegrityAsync(_tenantId);
                fingerprint1 = report.JournalFingerprint;
                Assert.Equal(1, report.TotalEntriesScanned);

                var cp = await context1.JournalCheckpoints.FirstOrDefaultAsync(c => c.TenantId == _tenantId);
                Assert.NotNull(cp);
                Assert.Equal(fingerprint1, cp.LastFingerprint);
            }

            using (var contextAlter = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
            {
                var entry = await contextAlter.LedgerEntries.FirstAsync();
                typeof(LedgerEntry).GetProperty("Debit")!.SetValue(entry, 999m);
                _ = await contextAlter.SaveChangesAsync();
            }

            using (var context2 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
            {
                var acc = await context2.LedgerAccounts.FirstAsync();
                var tx2 = new LedgerTransaction(_tenantId, "Block 2", "B2");
                tx2.AddEntry(acc.Id, 200, 200);
                tx2.Post();

                var entry = tx2.Entries.First();
                typeof(LedgerEntry).GetProperty("JournalSequenceId")!.SetValue(entry, 2L);

                _ = context2.LedgerTransactions.Add(tx2);
                _ = await context2.SaveChangesAsync();
            }

            using (var context3 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
            {
                var service = new LedgerIntegrityService(context3, new Mock<ILedgerHealthCache>().Object, new Mock<ILedgerHasher>().Object, _clockMock.Object, new Mock<ILedgerAnchorStore>().Object, Mock.Of<ILogger<LedgerIntegrityService>>());

                var report = await service.VerifyJournalIntegrityAsync(_tenantId);

                Assert.True(report.IsHealthy);
                Assert.Equal(1, report.TotalEntriesScanned);
            }

            using (var context4 = new CobryxDbContext(_dbOptions, _tenantProviderMock.Object))
            {
                var service = new LedgerIntegrityService(
                    context4,
                    new Mock<ILedgerHealthCache>().Object,
                    new Mock<ILedgerHasher>().Object,
                    _clockMock.Object,
                    new Mock<ILedgerAnchorStore>().Object,
                    Mock.Of<ILogger<LedgerIntegrityService>>());

                var report = await service.VerifyJournalIntegrityAsync(_tenantId, forceFullReplay: true);

                Assert.NotEqual(fingerprint1, report.JournalFingerprint);
                Assert.Equal(1, report.Violations.Count(v => v.Type == "IMBALANCE"));
                Assert.False(report.IsHealthy);
            }
        }

        public void Dispose() => throw new NotImplementedException();
    }
}
