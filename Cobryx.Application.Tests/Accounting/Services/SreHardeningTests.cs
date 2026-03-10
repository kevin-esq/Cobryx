using Cobryx.Application.Accounting.Events;
using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Accounting;
using Cobryx.Infrastructure.BackgroundJobs.Accounting;
using Cobryx.Infrastructure.Services.Accounting;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Xunit;

namespace Cobryx.Application.Tests.Accounting.Services;

public class SreHardeningTests
{
    private readonly CobryxMetrics _metrics = new();
    private readonly Mock<ILedgerBalanceService> _mockBalanceService = new();
    private readonly Mock<ILogger<ShadowReplayEngine>> _loggerSre = new();
    private readonly Mock<ILogger<DriftDetectionWorker>> _loggerDrift = new();

    private readonly Mock<ITenantProvider> _mockTenantProvider = new();

    private CobryxDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CobryxDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new CobryxDbContext(options, _mockTenantProvider.Object);
    }

    [Fact]
    public async Task DriftDetection_ShouldTripCircuitBreaker_OnMismatch()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        using (var context = CreateDbContext(dbName))
        {
            var tenant = new Tenant("Test", "test.com");
            typeof(BaseEntity).GetProperty("Id")!.SetValue(tenant, tenantId);
            context.Tenants.Add(tenant);

            var shadow = new ShadowBalance(tenantId, accountId, 1000m, 500L);
            context.ShadowBalances.Add(shadow);

            var entry = (LedgerEntry)Activator.CreateInstance(typeof(LedgerEntry), true)!;
            typeof(LedgerEntry).GetProperty("TenantId")!.SetValue(entry, tenantId);
            typeof(LedgerEntry).GetProperty("AccountId")!.SetValue(entry, accountId);
            typeof(LedgerEntry).GetProperty("JournalSequenceId")!.SetValue(entry, 500L);
            context.LedgerEntries.Add(entry);

            await context.SaveChangesAsync();

            // Simulate a drift: Ledger thinks balance is 1200, Shadow thinks 1000
            _mockBalanceService.Setup(b => b.GetHistoricalBalanceAsync(tenantId, accountId, 500L, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BalanceResult(1200m, 500L));

            var worker = new DriftDetectionWorker(context, _mockBalanceService.Object, _metrics, _loggerDrift.Object);

            // Act
            await worker.ExecuteAsync();

            // Assert
            var updatedTenant = await context.Tenants.FindAsync(tenantId);
            Assert.True(updatedTenant!.FinancialSafeMode);
        }
    }
}

// Minimal async testing helpers (usually provided in test base)
internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
    public T Current => _inner.Current;
}

internal class TestAsyncQueryProvider<TEntity> : Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;
    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression) => new TestAsyncEnumerable<TEntity>(expression);
    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression) => new TestAsyncEnumerable<TElement>(expression);
    public object Execute(System.Linq.Expressions.Expression expression) => _inner.Execute(expression)!;
    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression) => _inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default) => Execute<TResult>(expression);
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}
