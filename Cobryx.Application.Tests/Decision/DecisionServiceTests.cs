using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision;
using Cobryx.Domain.Decision;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cobryx.Application.Tests.Decision;

public class DecisionServiceTests
{
    [Fact]
    public async Task ShouldCacheDecision()
    {
        var cacheMock = new Mock<ICacheService>();
        var dbMock = new Mock<ICobryxDbContext>();
        var dbSetMock = new Mock<DbSet<DecisionSnapshot>>();
        
        dbMock.Setup(d => d.DecisionSnapshots).Returns(dbSetMock.Object);

        var cacheStore = new Dictionary<string, DecisionResult>();
        
        cacheMock.Setup(x => x.GetAsync<DecisionResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((string k, CancellationToken c) => cacheStore.TryGetValue(k, out var v) ? v : null);
                 
        cacheMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<DecisionResult>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                 .Callback((string k, DecisionResult v, TimeSpan? t, CancellationToken c) => cacheStore[k] = v)
                 .Returns(Task.CompletedTask);

        var engine = new DecisionEngine(
            new CreditLimitEngine(),
            new PricingEngine(),
            new FraudEngine()
        );

        var service = new DecisionService(engine, cacheMock.Object, dbMock.Object);
        
        var customerId = Guid.NewGuid();
        var ctx = new DecisionContext
        {
            Credit = new CreditContext { ProbabilityOfDefault = 0.1m, BehaviorScore = 1m, MonthlyIncomeEstimate = 10000m, Utilization = 0.5m },
            Pricing = new PricingContext { ProbabilityOfDefault = 0.1m },
            Fraud = new FraudContext()
        };

        var result1 = await service.EvaluateAsync(customerId, ctx);
        var result2 = await service.EvaluateAsync(customerId, ctx);

        // Verify save and calculate hit only once
        dbSetMock.Verify(x => x.Add(It.IsAny<DecisionSnapshot>()), Times.Once);
        dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<DecisionResult>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
        
        // Ensure result is exactly cached
        Assert.Equal(result1.CreditLimit, result2.CreditLimit);
        Assert.Equal(result1.InterestRate, result2.InterestRate);
    }
}
