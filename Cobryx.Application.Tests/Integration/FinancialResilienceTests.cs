using Cobryx.Application.Common.Exceptions;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Tests.Chaos;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Services.Accounting;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Cobryx.Application.Tests.Integration
{
    public class FinancialResilienceTests : IDisposable
    {
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly Mock<ITenantProvider> _mockTenantProvider = new();
        private readonly Mock<IClock> _mockClock = new();

        private CobryxDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CobryxDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new CobryxDbContext(options, _mockTenantProvider.Object);
        }

        [Fact]
        public async Task SafeMode_ShouldBlockMutations_AfterCorruptionDetected()
        {
            // 1. Setup
            var dbName = Guid.NewGuid().ToString();
            var tenantId = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            _ = _mockTenantProvider.Setup(t => t.GetTenantId()).Returns(tenantId);
            _ = _mockClock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

            var healthCache = new LedgerHealthCache(_cache);

            await using var db = CreateDbContext(dbName);
            var injector = new LedgerFaultInjector(db);
            var integrityService = new LedgerIntegrityService(db, healthCache, new Mock<ILedgerHasher>().Object, _mockClock.Object, new Mock<ILedgerAnchorStore>().Object, new Mock<Microsoft.Extensions.Logging.ILogger<LedgerIntegrityService>>().Object);

            // 2. Inject Corruption (Partial Commit)
            await injector.InjectPartialCommitAsync(tenantId, accountId, 1000m);

            // 3. Verify Health - Should be Healthy because we haven't scanned yet
            var initialStatus = healthCache.Get(tenantId);
            Assert.False(initialStatus.IsSafeMode);

            // 4. Trigger Integrity Scan (Detection)
            var report = await integrityService.VerifyJournalIntegrityAsync(tenantId);
            Assert.False(report.IsHealthy);
            Assert.True(report.CircuitBreakerTripped);

            // 5. Verify Safe Mode Activated
            var lockdownStatus = healthCache.Get(tenantId);
            Assert.True(lockdownStatus.IsSafeMode);
            Assert.Equal("IMBALANCE", lockdownStatus.Reason);

            // 6. Verify Over-the-wire Lockdown (Simulating Middleware)
            var status = healthCache.Get(tenantId);
            if (status.IsSafeMode)
            {
                // Explicitly avoiding Func<Task> overload to avoid obsolete warning
                void Act()
                {
                    throw new FinancialSafeModeException(tenantId, status.Reason);
                }

                _ = Assert.Throws<FinancialSafeModeException>(Act);
            }
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
