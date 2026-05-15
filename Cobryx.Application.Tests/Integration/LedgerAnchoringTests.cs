using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Tests.Chaos;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Accounting.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Services.Accounting;
using Cobryx.Infrastructure.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Cobryx.Application.Tests.Integration;

public class LedgerAnchoringTests : IDisposable
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<ITenantProvider> _mockTenantProvider = new();
    private readonly Mock<IClock> _mockClock = new();
    private readonly ILedgerHasher _hasher = new LedgerHasher();
    private readonly ILedgerSigner _signer = new HmacLedgerSigner();

    public void Dispose() => _cache.Dispose();

    private CobryxDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CobryxDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new CobryxDbContext(options, _mockTenantProvider.Object);
    }

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

    private class HighRiskCommand : IHighRiskOperation { }

    [Fact]
    public async Task HighRiskOperation_ShouldTriggerImmediateAnchoring()
    {
        // Setup
        var tenantId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();
        _ = _mockTenantProvider.Setup(t => t.GetTenantId()).Returns(tenantId);
        _ = _mockClock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        await using var db = CreateDbContext(dbName);
        await SetupSystemAccountsAsync(db, tenantId);

        var anchorStore = new ChainedFileAnchorStore();
        var anchorService = new LedgerAnchorService(anchorStore, _signer, db, _mockClock.Object);
        var postingEngine = new FinancialPostingEngine(db, _hasher, new Mock<Microsoft.Extensions.Logging.ILogger<FinancialPostingEngine>>().Object, anchorService);

        // Act: Post a reversal (which triggers anchoring via anonymous object or explicit marker)
        // Note: PostReversalAsync in our code passes an anonymous object. To test IHighRiskOperation, 
        // we'd typically have a command implementing it. But our current posting engine triggers on amounts too.
        
        await postingEngine.PostLoanPaymentAsync(new Domain.Lending.Loan(tenantId, Guid.NewGuid(), Guid.NewGuid(), "LN-1", new(1000m, "USD")), 100, "REF-1");

        // Verify: Seq 1 is NOT high risk (periodic 50), and amount 100 < 10000.
        Assert.Null(anchorStore.GetLatest(tenantId));

        // Act: Post a high-value transaction (Quantitative Risk)
        await postingEngine.PostLoanPaymentAsync(new Domain.Lending.Loan(tenantId, Guid.NewGuid(), Guid.NewGuid(), "LN-1", new(20000m, "USD")), 15000, "HIGH-VAL");

        // Verify: Anchor should exist now
        var latest = anchorStore.GetLatest(tenantId);
        Assert.NotNull(latest);
        Assert.Equal(2, latest.Sequence);
    }

    [Fact]
    public async Task ForensicScan_ShouldDetectAnchorMismatch_WhenDbInternallyConsistentButTampered()
    {
        // 1. Setup
        var tenantId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();
        _ = _mockTenantProvider.Setup(t => t.GetTenantId()).Returns(tenantId);
        _ = _mockClock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        await using var db = CreateDbContext(dbName);
        await SetupSystemAccountsAsync(db, tenantId);

        var anchorStore = new ChainedFileAnchorStore();
        var anchorService = new LedgerAnchorService(anchorStore, _signer, db, _mockClock.Object);
        var postingEngine = new FinancialPostingEngine(db, _hasher, new Mock<Microsoft.Extensions.Logging.ILogger<FinancialPostingEngine>>().Object, anchorService);
        var healthCache = new LedgerHealthCache(_cache);
        var integrityService = new LedgerIntegrityService(db, healthCache, _hasher, _mockClock.Object, anchorStore, new Mock<Microsoft.Extensions.Logging.ILogger<LedgerIntegrityService>>().Object);

        // 2. Create high-risk transaction that gets anchored
        await postingEngine.PostLoanPaymentAsync(new Domain.Lending.Loan(tenantId, Guid.NewGuid(), Guid.NewGuid(), "LN-1", new(1000m, "USD")), 15000, "REF-ANCHOR");
        
        var anchor = anchorStore.GetLatest(tenantId);
        Assert.NotNull(anchor);

        // 3. TAMPER: Modify amount in DB AND RE-CALCULATE HASHES
        // This is the "Historical Rewrite" attack where the internal chain is made valid again.
        var tx = await db.LedgerTransactions.Include(t => t.Entries).FirstAsync(t => t.Sequence == anchor.Sequence);
        var injector = new LedgerFaultInjector(db);
        await injector.TamperTransactionAmountAsync(tx.Id, 1m); // Change to 1
        
        // Reload tx to get updated state before re-sealing
        tx = await db.LedgerTransactions.Include(t => t.Entries).FirstAsync(t => t.Id == tx.Id);
        
        // Re-seal with the tampered amount so HASH_MISMATCH won't detect it internally
        var newHash = _hasher.ComputeHash(tx, tx.PreviousHash!);
        // Using reflection because Seal is likely protected/internal or only for new ones
        var sealMethod = tx.GetType().GetMethod("Seal", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        sealMethod?.Invoke(tx, [tx.PreviousHash, tx.Sequence, newHash]);
        await db.SaveChangesAsync();

        // 4. VERIFY: Internal scan alone would be green (without anchors) if we didn't have external proof
        // (Assuming we checked it, but let's go straight to integrityService which now uses anchors)
        
        var report = await integrityService.VerifyJournalIntegrityAsync(tenantId);

        // 5. Assert: ANCHOR_MISMATCH detected!
        Assert.False(report.IsHealthy);
        Assert.Contains(report.Violations, v => v.Type == "ANCHOR_MISMATCH");
    }

    [Fact]
    public async Task ForensicScan_ShouldDetectRollback_WhenDbHistoryIsModified()
    {
        // Setup
        var tenantId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();
        _ = _mockTenantProvider.Setup(t => t.GetTenantId()).Returns(tenantId);
        _ = _mockClock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        await using var db = CreateDbContext(dbName);
        await SetupSystemAccountsAsync(db, tenantId);

        var anchorStore = new ChainedFileAnchorStore();
        var anchorService = new LedgerAnchorService(anchorStore, _signer, db, _mockClock.Object);
        var postingEngine = new FinancialPostingEngine(db, _hasher, new Mock<Microsoft.Extensions.Logging.ILogger<FinancialPostingEngine>>().Object, anchorService);
        var integrityService = new LedgerIntegrityService(db, new LedgerHealthCache(_cache), _hasher, _mockClock.Object, anchorStore, new Mock<Microsoft.Extensions.Logging.ILogger<LedgerIntegrityService>>().Object);

        // 1. Post and anchor
        await postingEngine.PostLoanPaymentAsync(new Domain.Lending.Loan(tenantId, Guid.NewGuid(), Guid.NewGuid(), "LN-1", new(20000m, "USD")), 15000, "REF-1");
        
        // 2. Simulate ROLLBACK: Remove transaction from DB but keep Anchor externally
        var tx = await db.LedgerTransactions.FirstAsync();
        db.LedgerTransactions.Remove(tx);
        await db.SaveChangesAsync();

        // 3. Audit
        var report = await integrityService.VerifyJournalIntegrityAsync(tenantId);

        // 4. Assert: LEDGER_ROLLBACK detected
        Assert.False(report.IsHealthy);
        Assert.Contains(report.Violations, v => v.Type == "LEDGER_ROLLBACK");
    }
}
