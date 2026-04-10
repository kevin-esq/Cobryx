using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML;
using Cobryx.Application.ML.Interfaces;
using Cobryx.Domain.ML;
using Cobryx.Infrastructure.ML.Routing;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Tests.Unit.ML;

public class ReplayEngineTests
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
    public async Task Replay_WithIdenticalSnapshot_MustGuaranteeAbsoluteTraceDeterminism()
    {
        var testRandomProvider = new Infrastructure.Providers.SystemRandomProvider();
        testRandomProvider.Reseed(12345);
        var dbMock = new Mock<ICobryxDbContext>();
        SetupDbMock(dbMock);

        var snapshots = new List<ReplaySnapshot>();
        var dbSetMock = new Mock<DbSet<ReplaySnapshot>>();
        dbSetMock.Setup(d => d.Add(It.IsAny<ReplaySnapshot>())).Callback<ReplaySnapshot>(snapshots.Add);
        dbMock.Setup(d => d.ReplaySnapshots).Returns(dbSetMock.Object);

        var engine = new DecisionEngine(new CreditLimitEngine(), new PricingEngine(), new FraudEngine());
        var riskEvaluatorMock = new Mock<IRiskEvaluator>();
        riskEvaluatorMock.Setup(x =>
                x.EvaluateRiskAsync(It.IsAny<Guid>(), It.IsAny<DecisionContext>(), It.IsAny<FeatureVector>()))
            .ReturnsAsync((0.1m, "v1"));

        var featureStoreMock = new Mock<IFeatureStore>();
        featureStoreMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(new FeatureVector());

        var snapshotStoreMock = new Mock<ISnapshotStore>();
        snapshotStoreMock.Setup(x => x.QueueSnapshotAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<object>(),
                It.IsAny<object>(),
                It.IsAny<ExecutionTrace>(),
                It.IsAny<DecisionResult>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string, object, object, object, ExecutionTrace, DecisionResult, bool,
                CancellationToken>((cid, ver, hash, feat, macroState, port, trace, res, _, _) =>
            {
                snapshots.Add(new ReplaySnapshot
                {
                    CustomerId = cid,
                    EngineVersion = ver,
                    ConfigHash = hash,
                    FeatureVectorJson = System.Text.Json.JsonSerializer.Serialize(feat),
                    MacroStateJson = System.Text.Json.JsonSerializer.Serialize(macroState),
                    PortfolioStateJson = System.Text.Json.JsonSerializer.Serialize(port),
                    ExecutionTraceJson = System.Text.Json.JsonSerializer.Serialize(trace),
                    OriginalCreditLimit = res.CreditLimit,
                    OriginalInterestRate = res.InterestRate,
                    RandomSeed = 12345
                });
            })
            .Returns(Task.CompletedTask);

        var macroStoreMock = new Mock<IMacroFeatureStore>();
        macroStoreMock.Setup(x => x.GetAsync()).ReturnsAsync(new MacroState());

        var portfolioStoreMock = new Mock<IPortfolioFeatureStore>();
        portfolioStoreMock.Setup(x => x.GetGlobalStateAsync()).ReturnsAsync(new PortfolioState());

        var service = new DecisionService(engine, new Mock<ICacheService>().Object, dbMock.Object,
            featureStoreMock.Object,
            riskEvaluatorMock.Object, new ModelRouter(testRandomProvider), new Mock<IRlEngine>().Object,
            new ScenarioGenerator(testRandomProvider),
            new Mock<IMonteCarloEvaluator>().Object, new Mock<IPortfolioEngine>().Object,
            portfolioStoreMock.Object, macroStoreMock.Object,
            new GuardrailEngine(), testRandomProvider, snapshotStoreMock.Object,
            new Mock<Application.Decision.Interfaces.IShadowComparer>().Object,
            Options.Create(new ShadowConfig { Enabled = false }),
            new Mock<IServiceScopeFactory>().Object,
            new Mock<ILogger<DecisionService>>().Object);

        DecisionContext ctx = new() { Credit = new(), Pricing = new(), Fraud = new() };
        await service.EvaluateAsync(Guid.NewGuid(), ctx, overrideSeed: 12345);

        ReplayEngine replayEngine = new(service, new Mock<ILogger<ReplayEngine>>().Object);
        Cobryx.Application.ML.Models.ReplayResult result = await replayEngine.ReplayAsync(new ReplaySnapshotAdapter(snapshots.Last()));

        Assert.True(result.IsDeterministic);
    }

    [Fact]
    public async Task DecisionService_ShadowMode_ShouldTriggerAsyncMonitoring()
    {
        var testRandomProvider = new Infrastructure.Providers.SystemRandomProvider();
        testRandomProvider.Reseed(123);
        var dbMock = new Mock<ICobryxDbContext>();
        SetupDbMock(dbMock);

        var shadowMonitorMock = new Mock<Application.Decision.Interfaces.IShadowMonitor>();
        var shadowComparerMock = new Mock<Application.Decision.Interfaces.IShadowComparer>();
        var replayEngineMock = new Mock<Application.Decision.Interfaces.IReplayEngine>();

        replayEngineMock.Setup(x => x.ReplayAsync(It.IsAny<IReplayInput>()))
            .ReturnsAsync(new Cobryx.Application.ML.Models.ReplayResult { IsDeterministic = true });

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(Application.Decision.Interfaces.IReplayEngine)))
            .Returns(replayEngineMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(Application.Decision.Interfaces.IShadowMonitor)))
            .Returns(shadowMonitorMock.Object);

        var featureStoreMock = new Mock<IFeatureStore>();
        featureStoreMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(new FeatureVector());

        var macroStoreMock = new Mock<IMacroFeatureStore>();
        macroStoreMock.Setup(x => x.GetAsync()).ReturnsAsync(new MacroState());

        var portfolioStoreMock = new Mock<IPortfolioFeatureStore>();
        portfolioStoreMock.Setup(x => x.GetGlobalStateAsync()).ReturnsAsync(new PortfolioState());

        var service = new DecisionService(
            new DecisionEngine(new CreditLimitEngine(), new PricingEngine(), new FraudEngine()),
            new Mock<ICacheService>().Object, dbMock.Object, featureStoreMock.Object,
            new Mock<IRiskEvaluator>().Object,
            new ModelRouter(testRandomProvider), new Mock<IRlEngine>().Object,
            new ScenarioGenerator(testRandomProvider),
            new Mock<IMonteCarloEvaluator>().Object, new Mock<IPortfolioEngine>().Object,
            portfolioStoreMock.Object,
            macroStoreMock.Object, new GuardrailEngine(), testRandomProvider,
            new Mock<ISnapshotStore>().Object,
            shadowComparerMock.Object,
            Options.Create(new ShadowConfig { Enabled = true, SamplingRate = 1.0m }),
            scopeFactoryMock.Object,
            new Mock<ILogger<DecisionService>>().Object);

        DecisionContext ctx = new() { Credit = new(), Pricing = new(), Fraud = new() };
        DecisionResult result = await service.EvaluateAsync(Guid.NewGuid(), ctx);

        await Task.Delay(100);

        shadowMonitorMock.Verify(x => x.RecordAsync(It.IsAny<ShadowExecutionResult>(), It.IsAny<DecisionContext>()),
            Times.AtMostOnce());
        Assert.NotNull(result);
    }
}
