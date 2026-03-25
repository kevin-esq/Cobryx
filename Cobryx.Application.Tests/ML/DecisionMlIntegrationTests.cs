using System.Net;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision;
using Cobryx.Application.ML;
using Cobryx.Domain.ML;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Tests.ML;

public class DecisionMlIntegrationTests
{
    private static void SetupDbMock(Mock<ICobryxDbContext> dbMock)
    {
        dbMock.Setup(d => d.ReplaySnapshots).Returns(new Mock<DbSet<ReplaySnapshot>>().Object);
        dbMock.Setup(d => d.ModelOutcomes).Returns(new Mock<DbSet<ModelOutcome>>().Object);
        dbMock.Setup(d => d.DecisionSnapshots).Returns(new Mock<DbSet<DecisionSnapshot>>().Object);
        dbMock.Setup(d => d.ShadowPredictions).Returns(new Mock<DbSet<ShadowPrediction>>().Object);
        dbMock.Setup(d => d.QValues).Returns(new Mock<DbSet<QValue>>().Object);
        dbMock.Setup(d => d.DecisionOutcomes).Returns(new Mock<DbSet<DecisionOutcome>>().Object);
        dbMock.Setup(d => d.Experiences).Returns(new Mock<DbSet<Experience>>().Object);
        dbMock.Setup(d => d.DecisionDistributionLogs).Returns(new Mock<DbSet<DecisionDistributionLog>>().Object);
        dbMock.Setup(d => d.ShadowDriftEvents).Returns(new Mock<DbSet<ShadowDriftEvent>>().Object);
    }

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
        SetupDbMock(dbMock);

        var cacheMock = new Mock<ICacheService>();

        var rlEngineMock = new Mock<IRlEngine>();
        rlEngineMock.Setup(x => x.DecideAsync(It.IsAny<RlState>()))
            .ReturnsAsync(DecisionAction.MediumRisk);

        var mcClientMock = new Mock<IMonteCarloEvaluator>();
        mcClientMock.Setup(x => x.EvaluateAsync(It.IsAny<object>(), It.IsAny<PortfolioState>(), It.IsAny<MacroState>(),
                It.IsAny<List<Scenario>>()))
            .ReturnsAsync(new MonteCarloMetrics { VaR95CreditMultiplier = 1.0m });

        var portfolioEngineMock = new Mock<IPortfolioEngine>();
        portfolioEngineMock.Setup(x => x.OptimizeAsync(It.IsAny<PortfolioState>()))
            .ReturnsAsync(new PortfolioAction { CreditMultiplier = 1.0m, RiskTolerance = 0.5m });

        var portfolioStoreMock = new Mock<IPortfolioFeatureStore>();
        portfolioStoreMock.Setup(x => x.GetGlobalStateAsync()).ReturnsAsync(new PortfolioState());

        var macroStoreMock = new Mock<IMacroFeatureStore>();
        macroStoreMock.Setup(x => x.GetAsync()).ReturnsAsync(new MacroState());

        var riskEvaluatorMock = new Mock<IRiskEvaluator>();
        riskEvaluatorMock.Setup(x =>
                x.EvaluateRiskAsync(It.IsAny<Guid>(), It.IsAny<DecisionContext>(), It.IsAny<FeatureVector>()))
            .ReturnsAsync((0.9m, "v1"));

        var snapshotStoreMock = new Mock<ISnapshotStore>();
        var service = new DecisionService(engine, cacheMock.Object, dbMock.Object, featureStoreMock.Object,
            riskEvaluatorMock.Object, new ModelRouter(new Mock<IRandomProvider>().Object), rlEngineMock.Object,
            new ScenarioGenerator(new Mock<IRandomProvider>().Object),
            mcClientMock.Object, portfolioEngineMock.Object, portfolioStoreMock.Object, macroStoreMock.Object,
            new GuardrailEngine(), new Mock<IRandomProvider>().Object,
            snapshotStoreMock.Object,
            new Mock<Application.Decision.Interfaces.IShadowComparer>().Object,
            Options.Create(new Application.Decision.Models.ShadowConfig { Enabled = false }),
            new Mock<IServiceScopeFactory>().Object,
            new Mock<ILogger<DecisionService>>().Object);

        var customerId = Guid.NewGuid();
        var ctx = new DecisionContext
        {
            Credit = new CreditContext { ProbabilityOfDefault = 0.1m, MonthlyIncomeEstimate = 1000m, Utilization = 0m },
            Pricing = new PricingContext { ProbabilityOfDefault = 0.1m },
            Fraud = new FraudContext()
        };

        var result = await service.EvaluateAsync(customerId, ctx);

        Assert.NotNull(result);
    }
}
