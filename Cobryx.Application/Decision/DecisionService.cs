using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML;
using Cobryx.Application.ML.Models;
using Cobryx.Domain.Config;
using Cobryx.Domain.Decision;
using Cobryx.Domain.ML;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Text.Json;

namespace Cobryx.Application.Decision
{
    public class DecisionService(
        DecisionEngine engine,
        ICacheService cache,
        ICobryxDbContext db,
        IFeatureStore featureStore,
        IRiskEvaluator riskEvaluator,
        ModelRouter router,
        IRlEngine rlEngine,
        ScenarioGenerator scenarioGenerator,
        IMonteCarloEvaluator monteCarlo,
        IPortfolioEngine portfolioEngine,
        IPortfolioFeatureStore portfolioStore,
        IMacroFeatureStore macroStore,
        GuardrailEngine guardrailEngine,
        IRandomProvider rng,
        ISnapshotStore snapshotStore,
        Interfaces.IShadowComparer shadowComparer,
        Microsoft.Extensions.Options.IOptions<ShadowConfig> shadowOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<DecisionService> logger)
    {
        public async Task<DecisionResult> EvaluateAsync(
            Guid customerId,
            DecisionContext ctx,
            FeatureVector? overrideFeatures = null,
            MacroState? overrideMacro = null,
            PortfolioState? overridePortfolio = null,
            int? overrideSeed = null,
            bool isReplay = false,
            CancellationToken ct = default)
        {
            var key = $"decision:{customerId}";
            DecisionResult? cached = isReplay ? null : await cache.GetAsync<DecisionResult>(key, ct);

            if (cached != null)
            {
                return cached;
            }

            if (overrideSeed.HasValue)
            {
                rng.Reseed(overrideSeed.Value);
            }

            var seed = overrideSeed ?? rng.Next();

            FeatureVector features = overrideFeatures ?? await featureStore.GetAsync(customerId);

            var (finalPd, prodVersion) = await riskEvaluator.EvaluateRiskAsync(customerId, ctx, features);

            var engineCtx = new DecisionContext
            {
                Credit = new CreditContext
                {
                    ProbabilityOfDefault = finalPd,
                    BehaviorScore = ctx.Credit.BehaviorScore,
                    MonthlyIncomeEstimate = ctx.Credit.MonthlyIncomeEstimate,
                    Utilization = ctx.Credit.Utilization
                },
                Pricing = new PricingContext
                {
                    ProbabilityOfDefault = finalPd
                },
                Fraud = new FraudContext
                {
                    TransactionsLastHour = ctx.Fraud.TransactionsLastHour,
                    AmountVelocity = ctx.Fraud.AmountVelocity,
                    GeoAnomaly = ctx.Fraud.GeoAnomaly
                }
            };

            var trace = new ExecutionTrace();
            trace.AddStep("Risk Evaluation", 0m, finalPd, $"Evaluated PD using {prodVersion}");

            DecisionResult result = engine.Evaluate(engineCtx);
            trace.AddStep("Base Rules Limit", 0m, result.CreditLimit, "Base credit limit evaluated");
            trace.AddStep("Base Rules Rate", 0m, result.InterestRate, "Base interest rate evaluated");

            var state = new RlState
            {
                PdBucket = Math.Round(finalPd, 1),
                UtilizationBucket = Math.Round(features.Utilization, 1),
                BehaviorBucket = Math.Round(features.BehaviorScore, 1)
            };

            var creditLimit = result.CreditLimit;
            var interestRate = result.InterestRate;

            DecisionAction? action = null;

            PortfolioState globalState = overridePortfolio ?? await portfolioStore.GetGlobalStateAsync();
            MacroState macro = overrideMacro ?? await macroStore.GetAsync();

            _ = await cache.GetAsync<PortfolioLimits>("portfolio:limits", ct) ??
                new PortfolioLimits { MaxExposure = RiskLimits.MaxPortfolioExposure };
            var globalCreditMultiplier = 1.0m;
            try
            {
                PortfolioAction portfolioAction = await portfolioEngine.OptimizeAsync(globalState);
                globalCreditMultiplier = portfolioAction.CreditMultiplier;
                var globalRiskTolerance = portfolioAction.RiskTolerance;
                trace.AddStep("Portfolio Engine", 1.0m, globalCreditMultiplier,
                    $"Applied macro risk tolerance: {globalRiskTolerance}");
            }
            catch (Exception)
            {
                /* ignored */
            }

            if (macro.InterestRate > 0.15m || macro.Inflation > 0.10m)
            {
                trace.AddStep("Macro Hard Stop", macro.InterestRate, 0m,
                    "Macro thresholds exceeded (Rate > 15% or Inflation > 10%)");
                result.Approved = false;
                result.CreditLimit = 0m;
                result.InterestRate = 0m;
                result.Trace = trace;
                return result;
            }

            if (router.UsePpo())
            {
                try
                {
                    var payloadFeatures = new
                    {
                        utilization = features.Utilization,
                        paymentDelay = features.PaymentDelay,
                        behaviorScore = features.BehaviorScore,
                        dpdTrend = features.DpdTrend,
                        outstanding = features.Outstanding
                    };

                    var scenarios = scenarioGenerator.Generate(macro);

                    MonteCarloMetrics mcMetrics = await monteCarlo.EvaluateAsync(
                        payloadFeatures,
                        globalState,
                        macro,
                        scenarios);

                    creditLimit *= globalCreditMultiplier;
                    creditLimit *= mcMetrics.VaR95CreditMultiplier;
                    interestRate += mcMetrics.AverageInterestDelta;

                    trace.AddStep("Monte Carlo Optimizer", globalCreditMultiplier, creditLimit,
                        $"Optimized via PPO. VaR95 Multiplier: {mcMetrics.VaR95CreditMultiplier}");
                }
                catch (Exception)
                {
                    /* fallback to standard logic */
                }
            }
            else
            {
                try
                {
                    action = await rlEngine.DecideAsync(state);
                    var oldLimit = creditLimit;
                    creditLimit = action.Value switch
                    {
                        DecisionAction.LowRisk => creditLimit * 1.2m,
                        DecisionAction.MediumRisk => creditLimit * 1.0m,
                        DecisionAction.HighRisk => creditLimit * 0.8m,
                        DecisionAction.Reject => 0m,
                        _ => creditLimit
                    };

                    if (action == DecisionAction.Reject)
                    {
                        result.Approved = false;
                    }

                    trace.AddStep("RL Engine", oldLimit, creditLimit,
                        $"Action {action.Value} applied via Q-Learning");
                }
                catch (Exception)
                {
                    /* ignored */
                }
            }

            var preGuardLimit = creditLimit;
            (creditLimit, interestRate) = guardrailEngine.Apply(creditLimit, interestRate, globalState);
            if (preGuardLimit != creditLimit)
            {
                trace.AddStep("Guardrail Override", preGuardLimit, creditLimit, "Hard guardrail enforced limits");
            }

            result.CreditLimit = creditLimit;
            result.InterestRate = interestRate;
            result.Trace = trace;

            if (!isReplay)
            {
                var outcome = new ModelOutcome(customerId, finalPd, prodVersion, false, 0m);
                _ = db.ModelOutcomes.Add(outcome);

                var dbSnapshot = new DecisionSnapshot(
                    customerId,
                    finalPd,
                    prodVersion,
                    result.CreditLimit,
                    result.InterestRate,
                    result.FraudScore
                );

                _ = db.DecisionSnapshots.Add(dbSnapshot);

                _ = db.DecisionOutcomes.Add(new DecisionOutcome
                {
                    CustomerId = customerId,
                    StateKey = state.ToKey(),
                    Action = action ?? DecisionAction.MediumRisk,
                    CreditLimit = creditLimit,
                    InterestRate = interestRate,
                    ModelVersion = prodVersion
                });

                var isFull = !result.Approved || result.CreditLimit == 0 || result.InterestRate > 0.20m ||
                             rng.NextDouble() < 0.05;

                _ = snapshotStore.QueueSnapshotAsync(
                    customerId,
                    EngineMetadata.EngineVersion,
                    EngineMetadata.ConfigHash,
                    features,
                    macro,
                    globalState,
                    trace,
                    result,
                    isFull,
                    ct);

                _ = await db.SaveChangesAsync(ct);

                await cache.SetAsync(key, result, TimeSpan.FromMinutes(5), ct);
            }

            if (shadowOptions.Value.Enabled && !isReplay && ShouldRunShadow(customerId))
            {
                var shadowInput = new ProductionSnapshotAdapter(new ProductionSnapshot(
                    customerId.ToString(),
                    EngineMetadata.EngineVersion,
                    EngineMetadata.ConfigHash,
                    trace.GetTraceHash(EngineMetadata.EngineVersion),
                    true,
                    result.CreditLimit,
                    result.InterestRate,
                    JsonSerializer.Serialize(features),
                    JsonSerializer.Serialize(macro),
                    JsonSerializer.Serialize(globalState),
                    JsonSerializer.Serialize(trace),
                    JsonSerializer.Serialize(result)
                ));

                _ = Task.Run(async () =>
                {
                    using var scope = scopeFactory.CreateScope();
                    var scopedReplayEngine = scope.ServiceProvider.GetRequiredService<Interfaces.IReplayEngine>();
                    var scopedMonitor = scope.ServiceProvider.GetRequiredService<Interfaces.IShadowMonitor>();
                    try
                    {
                        var shadowReplayResult = await scopedReplayEngine.ReplayAsync(shadowInput);
                        var primaryReplayResult = new ReplayResult
                        {
                            IsDeterministic = true,
                            DeltaCreditLimit = 0,
                            DeltaInterestRate = 0,
                            OriginalEngineVersion = EngineMetadata.EngineVersion,
                            ReplayedEngineVersion = EngineMetadata.EngineVersion,
                            OriginalTraceHash = trace.GetTraceHash(EngineMetadata.EngineVersion),
                            ReplayedTraceHash = trace.GetTraceHash(EngineMetadata.EngineVersion)
                        };

                        var shadowResult = shadowComparer.Compare(
                            Guid.NewGuid(),
                            primaryReplayResult,
                            shadowReplayResult,
                            new DriftToleranceProfile());

                        await scopedMonitor.RecordAsync(shadowResult, engineCtx);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Shadow evaluation failed for {CustomerId}", customerId);
                    }
                }, CancellationToken.None);
            }

            return result;
        }

        private bool ShouldRunShadow(Guid id)
        {
            var hash = BitConverter.ToUInt32(System.Security.Cryptography.SHA256.HashData(id.ToByteArray()), 0);
            return (hash % 100) < (uint)(shadowOptions.Value.SamplingRate * 100);
        }
    }
}
