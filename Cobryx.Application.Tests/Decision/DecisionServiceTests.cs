using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision;
using Cobryx.Domain.Decision;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;

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
        dbMock.Setup(d => d.ModelOutcomes).Returns(new Mock<DbSet<Cobryx.Domain.ML.ModelOutcome>>().Object);

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

        var featureStoreMock = new Mock<Cobryx.Application.ML.IFeatureStore>();
        featureStoreMock.Setup(x => x.GetAsync(It.IsAny<System.Guid>())).ReturnsAsync(new Cobryx.Domain.ML.FeatureVector());

        var mockHttp = new Mock<System.Net.Http.HttpMessageHandler>();
        mockHttp.Protected()
            .Setup<System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<System.Net.Http.HttpRequestMessage>(),
                ItExpr.IsAny<System.Threading.CancellationToken>()
            )
            .ReturnsAsync(new System.Net.Http.HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new System.Net.Http.StringContent("{\"ProbabilityOfDefault\": 0.1}")
            });

        var httpClient = new System.Net.Http.HttpClient(mockHttp.Object) { BaseAddress = new System.Uri("http://dummy") };
        var mlClient = new Cobryx.Application.ML.MlClient(httpClient);

        var service = new DecisionService(engine, cacheMock.Object, dbMock.Object, featureStoreMock.Object, mlClient);
        
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
    [Fact]
    public async Task MlFailure_ShouldFallbackToHeuristic()
    {
        var cacheMock = new Mock<ICacheService>();
        var dbMock = new Mock<ICobryxDbContext>();
        var dbSetMock = new Mock<DbSet<DecisionSnapshot>>();
        DecisionSnapshot capturedSnapshot = default!;

        dbSetMock.Setup(d => d.Add(It.IsAny<DecisionSnapshot>())).Callback<DecisionSnapshot>(s => capturedSnapshot = s);
        dbMock.Setup(d => d.DecisionSnapshots).Returns(dbSetMock.Object);
        dbMock.Setup(d => d.ModelOutcomes).Returns(new Mock<DbSet<Cobryx.Domain.ML.ModelOutcome>>().Object);

        var engine = new DecisionEngine(new CreditLimitEngine(), new PricingEngine(), new FraudEngine());
        var featureStoreMock = new Mock<Cobryx.Application.ML.IFeatureStore>();
        featureStoreMock.Setup(x => x.GetAsync(It.IsAny<System.Guid>())).ReturnsAsync(new Cobryx.Domain.ML.FeatureVector());

        // Simulate failing HTTP Request (e.g., Timeout or 500)
        var mockHttp = new Mock<System.Net.Http.HttpMessageHandler>();
        mockHttp.Protected()
            .Setup<System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<System.Net.Http.HttpRequestMessage>(),
                ItExpr.IsAny<System.Threading.CancellationToken>()
            )
            .ThrowsAsync(new System.Net.Http.HttpRequestException("ML Service Unreachable"));

        var httpClient = new System.Net.Http.HttpClient(mockHttp.Object) { BaseAddress = new System.Uri("http://dummy") };
        var mlClient = new Cobryx.Application.ML.MlClient(httpClient);

        var service = new DecisionService(engine, cacheMock.Object, dbMock.Object, featureStoreMock.Object, mlClient);

        var ctx = new DecisionContext { Credit = new CreditContext { ProbabilityOfDefault = 0.2m }, Pricing = new PricingContext(), Fraud = new FraudContext() };

        await service.EvaluateAsync(Guid.NewGuid(), ctx);

        Assert.NotNull(capturedSnapshot);
        Assert.Equal("fallback-heuristic", capturedSnapshot.ModelVersion);
    }
}
