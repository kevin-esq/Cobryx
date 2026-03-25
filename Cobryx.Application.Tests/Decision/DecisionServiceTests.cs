using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision;
using Cobryx.Application.ML;
using Cobryx.Domain.ML;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Tests.Decision;

public class DecisionServiceTests
{
    private static Mock<ICobryxDbContext> CreateDbMock()
    {
        var dbMock = new Mock<ICobryxDbContext>();
        dbMock.Setup(d => d.DecisionSnapshots).Returns(new Mock<DbSet<DecisionSnapshot>>().Object);
        dbMock.Setup(d => d.ModelOutcomes).Returns(new Mock<DbSet<ModelOutcome>>().Object);
        dbMock.Setup(d => d.ShadowPredictions).Returns(new Mock<DbSet<ShadowPrediction>>().Object);
        dbMock.Setup(d => d.DecisionOutcomes).Returns(new Mock<DbSet<DecisionOutcome>>().Object);
        dbMock.Setup(d => d.QValues).Returns(new Mock<DbSet<QValue>>().Object);
        dbMock.Setup(d => d.Experiences).Returns(new Mock<DbSet<Experience>>().Object);
        dbMock.Setup(d => d.ReplaySnapshots).Returns(new Mock<DbSet<ReplaySnapshot>>().Object);
        dbMock.Setup(d => d.DecisionDistributionLogs).Returns(new Mock<DbSet<DecisionDistributionLog>>().Object);
        dbMock.Setup(d => d.ShadowDriftEvents).Returns(new Mock<DbSet<ShadowDriftEvent>>().Object);
        return dbMock;
    }

    [Fact]
    public async Task ShouldCacheDecision()
    {
        var cacheMock = new Mock<ICacheService>();
        var dbMock = CreateDbMock();
        var dbSetMock = new Mock<DbSet<DecisionSnapshot>>();
        dbMock.Setup(d => d.DecisionSnapshots).Returns(dbSetMock.Object);

        var cacheStore = new Dictionary<string, DecisionResult>();
        cacheMock.Setup(x => x.GetAsync<DecisionResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string k, CancellationToken _) => cacheStore.TryGetValue(k, out var v) ? v : null);
        cacheMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<DecisionResult>(), It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Callback((string k, DecisionResult v, TimeSpan? _, CancellationToken _) => cacheStore[k] = v)
            .Returns(Task.CompletedTask);

        var engine = new DecisionEngine(new CreditLimitEngine(), new PricingEngine(), new FraudEngine());
        var featureStoreMock = new Mock<IFeatureStore>();
        featureStoreMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(new FeatureVector());

        var riskEvaluatorMock = new Mock<IRiskEvaluator>();
        riskEvaluatorMock.Setup(x =>
                x.EvaluateRiskAsync(It.IsAny<Guid>(), It.IsAny<DecisionContext>(), It.IsAny<FeatureVector>()))
            .ReturnsAsync((0.1m, "v1"));

        var service = new DecisionService(
            engine, cacheMock.Object, dbMock.Object, featureStoreMock.Object,
            riskEvaluatorMock.Object, new ModelRouter(new Mock<IRandomProvider>().Object), new Mock<IRlEngine>().Object,
            new ScenarioGenerator(new Mock<IRandomProvider>().Object), new Mock<IMonteCarloEvaluator>().Object,
            new Mock<IPortfolioEngine>().Object, new Mock<IPortfolioFeatureStore>().Object,
            new Mock<IMacroFeatureStore>().Object,
            new GuardrailEngine(), new Mock<IRandomProvider>().Object, new Mock<ISnapshotStore>().Object,
            new Mock<Application.Decision.Interfaces.IShadowComparer>().Object,
            Options.Create(new Application.Decision.Models.ShadowConfig { Enabled = false }),
            new Mock<IServiceScopeFactory>().Object,
            new Mock<ILogger<DecisionService>>().Object);

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

        dbSetMock.Verify(x => x.Add(It.IsAny<DecisionSnapshot>()), Times.Once);
        cacheMock.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<DecisionResult>(), It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(result1.CreditLimit, result2.CreditLimit);
    }

    [Fact]
    public async Task MlFailure_ShouldFallbackToHeuristic()
    {
        var cacheMock = new Mock<ICacheService>();
        var dbMock = CreateDbMock();
        var dbSetMock = new Mock<DbSet<DecisionSnapshot>>();
        DecisionSnapshot capturedSnapshot = default!;
        dbSetMock.Setup(d => d.Add(It.IsAny<DecisionSnapshot>())).Callback<DecisionSnapshot>(s => capturedSnapshot = s);
        dbMock.Setup(d => d.DecisionSnapshots).Returns(dbSetMock.Object);

        var engine = new DecisionEngine(new CreditLimitEngine(), new PricingEngine(), new FraudEngine());
        var featureStoreMock = new Mock<IFeatureStore>();
        featureStoreMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(new FeatureVector());

        var riskEvaluatorMock = new Mock<IRiskEvaluator>();
        riskEvaluatorMock.Setup(x =>
                x.EvaluateRiskAsync(It.IsAny<Guid>(), It.IsAny<DecisionContext>(), It.IsAny<FeatureVector>()))
            .ReturnsAsync((0.2m, "fallback-heuristic"));

        var service = new DecisionService(
            engine, cacheMock.Object, dbMock.Object, featureStoreMock.Object,
            riskEvaluatorMock.Object, new ModelRouter(new Mock<IRandomProvider>().Object), new Mock<IRlEngine>().Object,
            new ScenarioGenerator(new Mock<IRandomProvider>().Object), new Mock<IMonteCarloEvaluator>().Object,
            new Mock<IPortfolioEngine>().Object, new Mock<IPortfolioFeatureStore>().Object,
            new Mock<IMacroFeatureStore>().Object,
            new GuardrailEngine(), new Mock<IRandomProvider>().Object, new Mock<ISnapshotStore>().Object,
            new Mock<Application.Decision.Interfaces.IShadowComparer>().Object,
            Options.Create(new Application.Decision.Models.ShadowConfig { Enabled = false }),
            new Mock<IServiceScopeFactory>().Object,
            new Mock<ILogger<DecisionService>>().Object);

        var ctx = new DecisionContext
        {
            Credit = new CreditContext { ProbabilityOfDefault = 0.2m },
            Pricing = new PricingContext(),
            Fraud = new FraudContext()
        };

        await service.EvaluateAsync(Guid.NewGuid(), ctx);

        Assert.NotNull(capturedSnapshot);
        Assert.Equal("fallback-heuristic", capturedSnapshot.ModelVersion);
    }
}
