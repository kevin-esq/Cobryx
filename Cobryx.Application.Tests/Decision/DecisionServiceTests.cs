using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision;
using Cobryx.Domain.Decision;
using Cobryx.Application.ML;

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
        dbMock.Setup(d => d.ModelOutcomes).Returns(new Mock<DbSet<Domain.ML.ModelOutcome>>().Object);
        dbMock.Setup(d => d.ShadowPredictions).Returns(new Mock<DbSet<Domain.ML.ShadowPrediction>>().Object);
        dbMock.Setup(d => d.DecisionOutcomes).Returns(new Mock<DbSet<Domain.ML.DecisionOutcome>>().Object);
        dbMock.Setup(d => d.QValues).Returns(new Mock<DbSet<Domain.ML.QValue>>().Object);
        dbMock.Setup(d => d.Experiences).Returns(new Mock<DbSet<Domain.ML.Experience>>().Object);

        var cacheStore = new Dictionary<string, DecisionResult>();

        cacheMock.Setup(x => x.GetAsync<DecisionResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string k, CancellationToken _) => cacheStore.TryGetValue(k, out var v) ? v : null);

        cacheMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<DecisionResult>(), It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Callback((string k, DecisionResult v, TimeSpan? _, CancellationToken _) => cacheStore[k] = v)
            .Returns(Task.CompletedTask);

        var engine = new DecisionEngine(
            new CreditLimitEngine(),
            new PricingEngine(),
            new FraudEngine()
        );

        var featureStoreMock = new Mock<IFeatureStore>();
        featureStoreMock.Setup(x => x.GetAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Cobryx.Domain.ML.FeatureVector());

        var mockHttp = new Mock<HttpMessageHandler>();
        mockHttp.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"ProbabilityOfDefault\": 0.1}")
            });

        var httpClient = new HttpClient(mockHttp.Object)
            { BaseAddress = new Uri("http://dummy") };
        var mlClient = new MlClient(httpClient);

        var rlEngineMock = new Mock<IRlEngine>();
        rlEngineMock.Setup(x => x.DecideAsync(It.IsAny<Cobryx.Domain.ML.RlState>()))
            .ReturnsAsync(Domain.ML.DecisionAction.MediumRisk);
        var mcPpoClientMock = new Mock<HttpMessageHandler>();
        mcPpoClientMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(
                    "{\"CreditMultipliers\":[1.0], \"InterestDeltas\":[0.0], \"LogProbs\":[0.0], \"Values\":[0.0]}")
            });
        var mcClient = new MonteCarloPpoClient(new HttpClient(mcPpoClientMock.Object) { BaseAddress = new Uri("http://dummy") });
        var scenarioGen = new ScenarioGenerator();
        var monteCarlo = new MonteCarloEvaluator(mcClient);

        var portfolioStoreMock = new Mock<IPortfolioFeatureStore>();
        portfolioStoreMock.Setup(x => x.GetGlobalStateAsync()).ReturnsAsync(new Cobryx.Domain.ML.PortfolioState
            { TotalExposure = 500000m, AvailableLiquidity = 500000m });
        var portfolioPpoClientMock = new Mock<HttpMessageHandler>();
        portfolioPpoClientMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(
                    "{\"CreditMultiplier\":1.0, \"RiskTolerance\":0.5, \"LiquidityBuffer\":0.1}")
            });
        var portfolioPpoClient = new PortfolioPpoClient(
            new HttpClient(portfolioPpoClientMock.Object) { BaseAddress = new Uri("http://dummy") });
        var portfolioEngine = new PortfolioEngine(portfolioPpoClient);

        var macroStoreMock = new Mock<IMacroFeatureStore>();
        macroStoreMock.Setup(x => x.GetAsync()).ReturnsAsync(new Cobryx.Domain.ML.MacroState
        {
            InterestRate = 0.05m, Inflation = 0.03m, CreditSpread = 0.02m, MarketVolatility = 0.15m
        });

        var service = new DecisionService(engine, cacheMock.Object, dbMock.Object, featureStoreMock.Object, mlClient,
            new ModelRouter(), new EnsembleService(), rlEngineMock.Object,
            scenarioGen, monteCarlo, portfolioEngine, portfolioStoreMock.Object, macroStoreMock.Object);

        var customerId = Guid.NewGuid();
        var ctx = new DecisionContext
        {
            Credit = new CreditContext
                { ProbabilityOfDefault = 0.1m, BehaviorScore = 1m, MonthlyIncomeEstimate = 10000m, Utilization = 0.5m },
            Pricing = new PricingContext { ProbabilityOfDefault = 0.1m },
            Fraud = new FraudContext()
        };

        var result1 = await service.EvaluateAsync(customerId, ctx);
        var result2 = await service.EvaluateAsync(customerId, ctx);

        // Verify save and calculate hit only once
        dbSetMock.Verify(x => x.Add(It.IsAny<DecisionSnapshot>()), Times.Once);
        dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<DecisionResult>(), It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()), Times.Once);

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
        dbMock.Setup(d => d.ModelOutcomes).Returns(new Mock<DbSet<Domain.ML.ModelOutcome>>().Object);
        dbMock.Setup(d => d.ShadowPredictions).Returns(new Mock<DbSet<Domain.ML.ShadowPrediction>>().Object);
        dbMock.Setup(d => d.DecisionOutcomes).Returns(new Mock<DbSet<Domain.ML.DecisionOutcome>>().Object);
        dbMock.Setup(d => d.QValues).Returns(new Mock<DbSet<Domain.ML.QValue>>().Object);

        var engine = new DecisionEngine(new CreditLimitEngine(), new PricingEngine(), new FraudEngine());
        var featureStoreMock = new Mock<IFeatureStore>();
        featureStoreMock.Setup(x => x.GetAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Cobryx.Domain.ML.FeatureVector());

        // Simulate failing HTTP Request (e.g., Timeout or 500)
        var mockHttp = new Mock<HttpMessageHandler>();
        mockHttp.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("ML Service Unreachable"));

        var httpClient = new HttpClient(mockHttp.Object)
            { BaseAddress = new Uri("http://dummy") };
        var mlClient = new MlClient(httpClient);

        var rlEngineMock = new Mock<IRlEngine>();
        rlEngineMock.Setup(x => x.DecideAsync(It.IsAny<Domain.ML.RlState>()))
            .ReturnsAsync(Domain.ML.DecisionAction.MediumRisk);
        var mcPpoClientMock = new Mock<HttpMessageHandler>();
        mcPpoClientMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(
                    "{\"CreditMultipliers\":[1.0], \"InterestDeltas\":[0.0], \"LogProbs\":[0.0], \"Values\":[0.0]}")
            });
        var mcClient = new MonteCarloPpoClient(new HttpClient(mcPpoClientMock.Object) { BaseAddress = new Uri("http://dummy") });
        var scenarioGen = new ScenarioGenerator();
        var monteCarlo = new MonteCarloEvaluator(mcClient);

        var portfolioStoreMock = new Mock<IPortfolioFeatureStore>();
        portfolioStoreMock.Setup(x => x.GetGlobalStateAsync()).ReturnsAsync(new Cobryx.Domain.ML.PortfolioState
            { TotalExposure = 500000m, AvailableLiquidity = 500000m });
        var portfolioPpoClientMock = new Mock<HttpMessageHandler>();
        portfolioPpoClientMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(
                    "{\"CreditMultiplier\":1.0, \"RiskTolerance\":0.5, \"LiquidityBuffer\":0.1}")
            });
        var portfolioPpoClient = new PortfolioPpoClient(
            new HttpClient(portfolioPpoClientMock.Object) { BaseAddress = new Uri("http://dummy") });
        var portfolioEngine = new PortfolioEngine(portfolioPpoClient);

        var macroStoreMock = new Mock<IMacroFeatureStore>();
        macroStoreMock.Setup(x => x.GetAsync()).ReturnsAsync(new Cobryx.Domain.ML.MacroState
        {
            InterestRate = 0.05m, Inflation = 0.03m, CreditSpread = 0.02m, MarketVolatility = 0.15m
        });

        var service = new DecisionService(engine, cacheMock.Object, dbMock.Object, featureStoreMock.Object, mlClient,
            new ModelRouter(), new EnsembleService(), rlEngineMock.Object,
            scenarioGen, monteCarlo, portfolioEngine, portfolioStoreMock.Object, macroStoreMock.Object);

        var ctx = new DecisionContext
        {
            Credit = new CreditContext { ProbabilityOfDefault = 0.2m }, Pricing = new PricingContext(),
            Fraud = new FraudContext()
        };

        await service.EvaluateAsync(Guid.NewGuid(), ctx);

        Assert.NotNull(capturedSnapshot);
        Assert.Equal("fallback-heuristic", capturedSnapshot.ModelVersion);
    }
}
