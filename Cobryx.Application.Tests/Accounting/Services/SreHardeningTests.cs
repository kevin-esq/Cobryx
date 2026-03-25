using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Shared;
using Cobryx.Infrastructure.BackgroundJobs.Accounting;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Cobryx.Application.Tests.Accounting.Services;

public class SreHardeningTests
{
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
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var metrics = new CobryxMetrics();
        var mockBalanceService = new Mock<ILedgerBalanceService>();
        var loggerDrift = new Mock<ILogger<DriftDetectionWorker>>();

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

            mockBalanceService.Setup(b =>
                    b.GetHistoricalBalanceAsync(tenantId, accountId, 500L, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BalanceResult(1200m, 500L));

            var worker = new DriftDetectionWorker(context, mockBalanceService.Object, metrics, loggerDrift.Object);

            await worker.ExecuteAsync();

            var updatedTenant = await context.Tenants.FindAsync(tenantId);
            Assert.True(updatedTenant!.FinancialSafeMode);
        }
    }
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
    public T Current => _inner.Current;
}

internal class TestAsyncQueryProvider<TEntity> : Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression) =>
        new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression) =>
        new TestAsyncEnumerable<TElement>(expression);

    public object Execute(System.Linq.Expressions.Expression expression) => _inner.Execute(expression)!;

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression) =>
        _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression,
        CancellationToken cancellationToken = default) => Execute<TResult>(expression);
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}
