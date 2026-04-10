using System.Reflection;

using Cobryx.Application.Analytics.Models;
using Cobryx.Application.Analytics.Queries.GetPortfolioAging;
using Cobryx.Application.Analytics.Queries.GetPortfolioSummary;
using Cobryx.Application.Collections.Queries.GetPriorityCases;
using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

namespace Cobryx.Application.Tests.Features.Collections;

/// <summary>
/// Tests for refactored Portfolio and Collections handlers.
/// Validates that the handlers correctly delegate to services and return expected results.
/// </summary>
public class CollectionsAndPortfolioTests
{
    private static readonly Guid TestTenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    #region Portfolio Summary Tests

    [Fact]
    public async Task GetPortfolioSummary_WithValidTenant_ReturnsCachedData()
    {
        // Arrange
        var expectedSummary = new PortfolioSummaryCache
        {
            TotalOutstanding = 1_000_000m,
            NplRatio = 0.05m,
            RevenueMTD = 50_000m,
            CollectionEfficiency = 0.85m
        };

        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns(TestTenantId);

        var cacheServiceMock = new Mock<ICacheService>();
        cacheServiceMock
            .Setup(x => x.GetAsync<PortfolioSummaryCache>(
                $"portfolio:summary:{TestTenantId}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        var handler = new GetPortfolioSummaryHandler(tenantProviderMock.Object, cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetPortfolioSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedSummary.TotalOutstanding, result.Value.TotalOutstanding);
        Assert.Equal(expectedSummary.NplRatio, result.Value.NplRatio);
        Assert.Equal(expectedSummary.RevenueMTD, result.Value.RevenueMTD);
        Assert.Equal(expectedSummary.CollectionEfficiency, result.Value.CollectionEfficiency);
    }

    [Fact]
    public void GetPortfolioSummaryQuery_IsTenantScoped()
    {
        // Assert - Query is marked [TenantScoped] and implements IRequiresTenant
        Assert.NotNull(typeof(GetPortfolioSummaryQuery).GetCustomAttribute<TenantScopedAttribute>());
        Assert.True(typeof(IRequiresTenant).IsAssignableFrom(typeof(GetPortfolioSummaryQuery)));
    }

    [Fact]
    public async Task GetPortfolioSummary_WithCacheMiss_ReturnsNotFound()
    {
        // Arrange
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns(TestTenantId);

        var cacheServiceMock = new Mock<ICacheService>();
        cacheServiceMock
            .Setup(x => x.GetAsync<PortfolioSummaryCache>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PortfolioSummaryCache?)null);

        var handler = new GetPortfolioSummaryHandler(tenantProviderMock.Object, cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetPortfolioSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCode.Common.EntityNotFound, result.Error);
    }

    #endregion

    #region Portfolio Aging Tests

    [Fact]
    public async Task GetPortfolioAging_WithValidTenant_ReturnsCachedData()
    {
        // Arrange
        var expectedAging = new PortfolioAgingCache
        {
            Current = 500_000m,
            Bucket0To30 = 200_000m,
            Bucket31To60 = 100_000m,
            Bucket61To90 = 50_000m,
            Bucket90Plus = 25_000m
        };

        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns(TestTenantId);

        var cacheServiceMock = new Mock<ICacheService>();
        cacheServiceMock
            .Setup(x => x.GetAsync<PortfolioAgingCache>(
                $"portfolio:aging:{TestTenantId}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAging);

        var handler = new GetPortfolioAgingHandler(tenantProviderMock.Object, cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetPortfolioAgingQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedAging.Current, result.Value.Current);
        Assert.Equal(expectedAging.Bucket90Plus, result.Value.Bucket90Plus);
    }

    #endregion

    #region Collections Priority Cases Tests

    [Fact]
    public async Task GetPriorityCases_WithValidTenant_ReturnsOrderedCases()
    {
        // Arrange
        var expectedCases = new List<PriorityCaseEntry>
        {
            new("loan-001", 95.5, new { CustomerName = "High Priority" }),
            new("loan-002", 85.0, new { CustomerName = "Medium Priority" }),
            new("loan-003", 75.0, new { CustomerName = "Lower Priority" })
        };

        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns(TestTenantId);

        var priorityStoreMock = new Mock<ICollectionsPriorityStore>();
        priorityStoreMock
            .Setup(x => x.GetTopPriorityCasesAsync(
                TestTenantId,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCases);

        var handler = new GetPriorityCasesHandler(priorityStoreMock.Object, tenantProviderMock.Object);

        // Act
        var result = await handler.Handle(new GetPriorityCasesQuery(100), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal(95.5, result.Value[0].PriorityScore);
        Assert.Equal(85.0, result.Value[1].PriorityScore);
    }

    [Fact]
    public void GetPriorityCasesQuery_IsTenantScoped()
    {
        // Assert - Query is marked [TenantScoped] and implements IRequiresTenant
        Assert.NotNull(typeof(GetPriorityCasesQuery).GetCustomAttribute<TenantScopedAttribute>());
        Assert.True(typeof(IRequiresTenant).IsAssignableFrom(typeof(GetPriorityCasesQuery)));
    }

    [Fact]
    public async Task GetPriorityCases_WithCustomLimit_PassesLimitToStore()
    {
        // Arrange
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns(TestTenantId);

        var priorityStoreMock = new Mock<ICollectionsPriorityStore>();
        priorityStoreMock
            .Setup(x => x.GetTopPriorityCasesAsync(
                TestTenantId,
                50, // Custom limit
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriorityCaseEntry>());

        var handler = new GetPriorityCasesHandler(priorityStoreMock.Object, tenantProviderMock.Object);

        // Act
        await handler.Handle(new GetPriorityCasesQuery(50), CancellationToken.None);

        // Assert
        priorityStoreMock.Verify(
            x => x.GetTopPriorityCasesAsync(TestTenantId, 50, It.IsAny<CancellationToken>()),
            Times.Once,
            "Handler should pass the custom limit to the store");
    }

    [Fact]
    public async Task GetPriorityCases_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns(TestTenantId);

        var priorityStoreMock = new Mock<ICollectionsPriorityStore>();
        priorityStoreMock
            .Setup(x => x.GetTopPriorityCasesAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriorityCaseEntry>());

        var handler = new GetPriorityCasesHandler(priorityStoreMock.Object, tenantProviderMock.Object);

        // Act
        var result = await handler.Handle(new GetPriorityCasesQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    #endregion

    #region Abstraction Validation Tests

    [Fact]
    public async Task CollectionsHandler_UsesAbstraction_NotDirectRedis()
    {
        // This test validates that the handler uses ICollectionsPriorityStore
        // and not direct Redis access (which was the original implementation)

        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns(TestTenantId);

        var priorityStoreMock = new Mock<ICollectionsPriorityStore>();
        priorityStoreMock
            .Setup(x => x.GetTopPriorityCasesAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriorityCaseEntry>());

        var handler = new GetPriorityCasesHandler(priorityStoreMock.Object, tenantProviderMock.Object);

        // Act
        await handler.Handle(new GetPriorityCasesQuery(), CancellationToken.None);

        // Assert - The fact that we can mock ICollectionsPriorityStore proves
        // the handler uses the abstraction, not direct Redis
        priorityStoreMock.Verify(
            x => x.GetTopPriorityCasesAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PortfolioHandler_UsesAbstraction_NotDirectCache()
    {
        // This test validates that the handler uses ICacheService
        // and not direct Redis access

        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns(TestTenantId);

        var cacheServiceMock = new Mock<ICacheService>();
        cacheServiceMock
            .Setup(x => x.GetAsync<PortfolioSummaryCache>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PortfolioSummaryCache());

        var handler = new GetPortfolioSummaryHandler(tenantProviderMock.Object, cacheServiceMock.Object);

        // Act
        await handler.Handle(new GetPortfolioSummaryQuery(), CancellationToken.None);

        // Assert
        cacheServiceMock.Verify(
            x => x.GetAsync<PortfolioSummaryCache>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
