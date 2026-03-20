using System.Net;

using Cobryx.Application.Decision;
using Cobryx.Application.ML;
using Cobryx.Domain.Decision;
using Cobryx.Domain.ML;
using Cobryx.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;

using Moq;
using Moq.Protected;

namespace Cobryx.Application.Tests.ML;

public class DecisionMlIntegrationTests
{
    [Fact]
    public async Task MlPrediction_ShouldInfluenceDecision()
    {
        var mockHttp = new Mock<HttpMessageHandler>();
        mockHttp.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"ProbabilityOfDefault\": 0.9}")
            });

        var httpClient = new HttpClient(mockHttp.Object) { BaseAddress = new Uri("http://dummy") };
        var mlClient = new MlClient(httpClient);

        var featureStoreMock = new Mock<IFeatureStore>();
        featureStoreMock.Setup(x => x.GetAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new FeatureVector { Outstanding = 1000m });

        var engine = new DecisionEngine(new CreditLimitEngine(), new PricingEngine(), new FraudEngine());
        var dbMock = new Mock<ICobryxDbContext>();
        dbMock.Setup(d => d.ModelOutcomes)
            .Returns(new Mock<DbSet<ModelOutcome>>().Object);
        dbMock.Setup(d => d.DecisionSnapshots)
            .Returns(new Mock<DbSet<DecisionSnapshot>>().Object);
        dbMock.Setup(d => d.ShadowPredictions)
            .Returns(new Mock<DbSet<ShadowPrediction>>().Object);
        dbMock.Setup(d => d.QValues)
            .Returns(new Mock<DbSet<QValue>>().Object);
        dbMock.Setup(d => d.DecisionOutcomes)
            .Returns(new Mock<DbSet<DecisionOutcome>>().Object);
        dbMock.Setup(d => d.Experiences)
            .Returns(new Mock<DbSet<Experience>>().Object);

        var cacheMock = new Mock<ICacheService>();

        var rlEngineMock = new Mock<IRlEngine>();
        rlEngineMock.Setup(x => x.DecideAsync(It.IsAny<RlState>()))
            .ReturnsAsync(DecisionAction.MediumRisk);
        var mcPpoClientMock = new Mock<HttpMessageHandler>();
        mcPpoClientMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "{\"CreditMultipliers\":[1.0], \"InterestDeltas\":[0.0], \"LogProbs\":[0.0], \"Values\":[0.0]}")
            });
        var mcClient = new MonteCarloPpoClient(new HttpClient(mcPpoClientMock.Object) { BaseAddress = new Uri("http://dummy") });
        var scenarioGen = new ScenarioGenerator();
        var monteCarlo = new MonteCarloEvaluator(mcClient);

        var portfolioStoreMock = new Mock<IPortfolioFeatureStore>();
        portfolioStoreMock.Setup(x => x.GetGlobalStateAsync()).ReturnsAsync(new PortfolioState
            { TotalExposure = 500000m, AvailableLiquidity = 500000m });
        var portfolioPpoClientMock = new Mock<HttpMessageHandler>();
        portfolioPpoClientMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "{\"CreditMultiplier\":1.0, \"RiskTolerance\":0.5, \"LiquidityBuffer\":0.1}")
            });
        var portfolioPpoClient = new PortfolioPpoClient(
            new HttpClient(portfolioPpoClientMock.Object) { BaseAddress = new Uri("http://dummy") });
        var portfolioEngine = new PortfolioEngine(portfolioPpoClient);

        var macroStoreMock = new Mock<IMacroFeatureStore>();
        macroStoreMock.Setup(x => x.GetAsync()).ReturnsAsync(new MacroState
        {
            InterestRate = 0.05m, Inflation = 0.03m, CreditSpread = 0.02m, MarketVolatility = 0.15m
        });

        var riskEvaluator = new DefaultRiskEvaluator(mlClient, new ModelRouter(), new EnsembleService(), dbMock.Object);
        var guardrailEngine = new GuardrailEngine();

        var service = new DecisionService(engine, cacheMock.Object, dbMock.Object, featureStoreMock.Object, riskEvaluator,
            new ModelRouter(), rlEngineMock.Object,
            scenarioGen, monteCarlo, portfolioEngine, portfolioStoreMock.Object, macroStoreMock.Object, guardrailEngine);

        var customerId = Guid.NewGuid();
        var ctx = new DecisionContext
        {
            Credit = new CreditContext { ProbabilityOfDefault = 0.1m, MonthlyIncomeEstimate = 1000m, Utilization = 0m },
            Pricing = new PricingContext { ProbabilityOfDefault = 0.1m },
            Fraud = new FraudContext()
        };

        var result = await service.EvaluateAsync(customerId, ctx);

        // Context PD (heuristic) was 0.1m. ML returned 0.9m.
        // Final PD = (0.1 * 0.3) + (0.9 * 0.7) = 0.03 + 0.63 = 0.66m.
        // So CreditEngine limit should be strictly constrained by high risk
        Assert.True(result.InterestRate > 0.15m); // Pricing reflects high risk
    }
}
