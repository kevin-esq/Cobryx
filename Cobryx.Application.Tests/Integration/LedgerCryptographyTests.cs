using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Tests.Chaos;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Services.Accounting;
using Cobryx.Infrastructure.Services.Security;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Cobryx.Application.Tests.Integration
{
    public class LedgerCryptographyTests : IDisposable
    {
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly Mock<ITenantProvider> _mockTenantProvider = new();
        private readonly Mock<IClock> _mockClock = new();
        private readonly ILedgerHasher _hasher = new LedgerHasher();

        public void Dispose() => _cache.Dispose();

        private static async Task SetupSystemAccountsAsync(CobryxDbContext db, Guid tenantId)
        {
            db.LedgerAccounts.AddRange(
                new LedgerAccount(tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available, "USD", true),
                new LedgerAccount(tenantId, "1210", "Principal", LedgerAccountType.Asset, LedgerAccountRole.Receivable, "USD", true),
                new LedgerAccount(tenantId, "4010", "Interest", LedgerAccountType.Revenue, LedgerAccountRole.None, "USD", true),
                new LedgerAccount(tenantId, "4020", "Fees", LedgerAccountType.Revenue, LedgerAccountRole.Fees, "USD", true),
                new LedgerAccount(tenantId, "5010", "Loss", LedgerAccountType.Expense, LedgerAccountRole.Loss, "USD", true),
                new LedgerAccount(tenantId, "4030", "Recovery", LedgerAccountType.Revenue, LedgerAccountRole.None, "USD", true)
            );
            _ = await db.SaveChangesAsync();
        }

        private CobryxDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CobryxDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new CobryxDbContext(options, _mockTenantProvider.Object);
        }

        [Fact]
        public async Task LedgerChainShouldPropagateBreakWhenHistoricalTransactionTampered()
        {
            // 1. Setup
            var dbName = Guid.NewGuid().ToString();
            var tenantId = Guid.NewGuid();
            var accountA = Guid.NewGuid();
            var accountB = Guid.NewGuid();
            _ = _mockTenantProvider.Setup(t => t.GetTenantId()).Returns(tenantId);
            _ = _mockClock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

            var healthCache = new LedgerHealthCache(_cache);

            await using var db = CreateDbContext(dbName);
            await SetupSystemAccountsAsync(db, tenantId);
            var injector = new LedgerFaultInjector(db);
            var postingEngine = new FinancialPostingEngine(db, _hasher, new Mock<Microsoft.Extensions.Logging.ILogger<FinancialPostingEngine>>().Object, new Mock<ILedgerAnchorService>().Object);
            var integrityService = new LedgerIntegrityService(db, healthCache, _hasher, _mockClock.Object, new Mock<ILedgerAnchorStore>().Object, new Mock<Microsoft.Extensions.Logging.ILogger<LedgerIntegrityService>>().Object);

            // 2. Create a Valid Chain of 5 Transactions
            var txIds = new List<Guid>();
            for (var i = 0; i < 5; i++)
            {
                var cid = Guid.NewGuid();
                var aid = Guid.NewGuid();
                var amount = new Domain.ValueObjects.Money(1000m, "USD");
                var txId = await postingEngine.PostLoanPaymentAsync(new Domain.Lending.Loan(tenantId, cid, aid, "LN-1", amount), 100, $"REF-{i}");
                txIds.Add(txId);
            }

            // 3. Verify Chain is initially healthy
            var initialReport = await integrityService.VerifyJournalIntegrityAsync(tenantId);
            Assert.True(initialReport.IsHealthy);

            // 4. TAINT: Silently tamper with Transaction #3 (Index 2)
            await injector.TamperTransactionAmountAsync(txIds[2], 999.99m);

            // 5. AUDIT: Full Forensic Scan
            var forensicReport = await integrityService.VerifyJournalIntegrityAsync(tenantId);

            // 6. VERIFY: Chain Breakdown Propagation
            Assert.False(forensicReport.IsHealthy);

            var violations = forensicReport.Violations;
            Assert.Contains(violations, static v => v.Type == "HASH_MISMATCH");
            Assert.Contains(violations, static v => v.Type == "CHAIN_BROKEN" || v.Type == "IMBALANCE");

            // 7. Verify Safe Mode Triggered
            Assert.True(healthCache.Get(tenantId).IsSafeMode);
        }

        [Fact]
        public async Task HashShouldDetectForensicChangesWhenTotalRemainsBalanced()
        {
            // Setup
            var dbName = Guid.NewGuid().ToString();
            var tenantId = Guid.NewGuid();
            _ = _mockTenantProvider.Setup(static t => t.GetTenantId()).Returns(tenantId);

            var healthCache = new LedgerHealthCache(_cache);

            await using var db = CreateDbContext(dbName);
            await SetupSystemAccountsAsync(db, tenantId);
            var injector = new LedgerFaultInjector(db);
            var postingEngine = new FinancialPostingEngine(db, _hasher, new Mock<Microsoft.Extensions.Logging.ILogger<FinancialPostingEngine>>().Object, new Mock<ILedgerAnchorService>().Object);
            var integrityService = new LedgerIntegrityService(db, healthCache, _hasher, _mockClock.Object, new Mock<ILedgerAnchorStore>().Object, new Mock<Microsoft.Extensions.Logging.ILogger<LedgerIntegrityService>>().Object);

            // 1. Create Valid Transaction
            var cid2 = Guid.NewGuid();
            var aid2 = Guid.NewGuid();
            var amount2 = new Domain.ValueObjects.Money(1000m, "USD");
            var txId = await postingEngine.PostLoanPaymentAsync(new Domain.Lending.Loan(tenantId, cid2, aid2, "LN-2", amount2), 500, "TAMPER-TEST");

            // 2. Tamper: Swap Debit/Credit or Change Account
            await injector.SwapEntriesAsync(txId);

            // 3. Audit
            var report = await integrityService.VerifyJournalIntegrityAsync(tenantId);

            // 4. Verify detection
            Assert.False(report.IsHealthy);
            Assert.Contains(report.Violations, static v => v.Type == "HASH_MISMATCH");
        }
    }
}
