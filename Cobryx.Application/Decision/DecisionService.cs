using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Decision;
using Cobryx.Domain.ML;
using Cobryx.Domain.Config;

namespace Cobryx.Application.Decision;

public class DecisionService(
    DecisionEngine engine,
    ICacheService cache,
    ICobryxDbContext db,
    ML.IFeatureStore featureStore,
    ML.MlClient mlClient,
    ML.ModelRouter router,
    ML.EnsembleService ensemble,
    ML.IRlEngine rlEngine,
    ML.ScenarioGenerator scenarioGenerator,
    ML.MonteCarloEvaluator monteCarlo,
    ML.PortfolioEngine portfolioEngine,
    ML.IPortfolioFeatureStore portfolioStore,
    ML.IMacroFeatureStore macroStore,
    ML.GuardrailEngine guardrailEngine)
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
        var seed = overrideSeed ?? new Random().Next();
        var key = $"decision:{customerId}";

        var cached = isReplay ? null : await cache.GetAsync<DecisionResult>(key, ct);

        if (cached != null)
            return cached;

        var features = overrideFeatures ?? await featureStore.GetAsync(customerId);

        var heuristicPd = ctx.Credit.ProbabilityOfDefault;
        decimal prodPd;
        string prodVersion;

        try
        {
            (prodPd, prodVersion) = await mlClient.PredictAsync(features, "xgb_v1");
        }
        catch
        {
            prodPd = heuristicPd;
            prodVersion = "fallback-heuristic";
        }

        decimal shadowPd = prodPd;

        if (router.ShouldRunShadow())
        {
            try
            {
                (shadowPd, var shadowVersion) = await mlClient.PredictAsync(features, "xgb_v2");

                db.ShadowPredictions.Add(new ShadowPrediction
                {
                    CustomerId = customerId,
                    ProductionPd = prodPd,
                    ShadowPd = shadowPd,
                    ProductionModelVersion = prodVersion,
                    ShadowModelVersion = shadowVersion,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception) { /* ignored */ }
        }

        var finalPd = ensemble.Combine(heuristicPd, prodPd, shadowPd);

        ctx.Credit.ProbabilityOfDefault = finalPd;
        ctx.Pricing.ProbabilityOfDefault = finalPd;

        var result = engine.Evaluate(ctx);

        var state = new RlState
        {
            PdBucket = Math.Round(finalPd, 1),
            UtilizationBucket = Math.Round(features.Utilization, 1),
            BehaviorBucket = Math.Round(features.BehaviorScore, 1)
        };

        var creditLimit = result.CreditLimit;
        var interestRate = result.InterestRate;

        DecisionAction? action = null;

        var globalState = overridePortfolio ?? await portfolioStore.GetGlobalStateAsync();
        var macro = overrideMacro ?? await macroStore.GetAsync();
        var limits = await cache.GetAsync<PortfolioLimits>("portfolio:limits", ct) ??
                     new PortfolioLimits { MaxExposure = RiskLimits.MaxPortfolioExposure };
        decimal globalCreditMultiplier = 1.0m;
        decimal globalRiskTolerance = 1.0m;
        try
        {
            var portfolioAction = await portfolioEngine.OptimizeAsync(globalState);
            globalCreditMultiplier = portfolioAction.CreditMultiplier;
            globalRiskTolerance = portfolioAction.RiskTolerance;
        }
        catch (Exception) { /* ignored */ }

        if (macro.InterestRate > 0.15m || macro.Inflation > 0.10m)
        {
            return new DecisionResult { Approved = false, CreditLimit = 0m, InterestRate = 0m };
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

                var scenarios = scenarioGenerator.Generate(macro, 50);

                var mcMetrics = await monteCarlo.EvaluateAsync(
                    payloadFeatures,
                    globalState,
                    macro,
                    scenarios);

                creditLimit *= globalCreditMultiplier;
                creditLimit *= mcMetrics.VaR95CreditMultiplier;
                interestRate += mcMetrics.AverageInterestDelta;

                if (finalPd > globalRiskTolerance)
                {
                    creditLimit *= 0.3m;
                }

                if (!isReplay)
                {
                    db.Experiences.Add(new Experience
                    {
                        CustomerId = customerId,
                        StateJson = System.Text.Json.JsonSerializer.Serialize(features),
                        CreditMultiplier = mcMetrics.AverageCreditMultiplier,
                        InterestDelta = mcMetrics.AverageInterestDelta,
                        LogProb = 0m,
                        Value = 0m,
                        Reward = 0m
                    });

                    var rawResultJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        creditMultipliers = mcMetrics.RawCreditMultipliers,
                        interestDeltas = mcMetrics.RawInterestDeltas
                    });

                    db.DecisionDistributionLogs.Add(new Domain.ML.DecisionDistributionLog
                    {
                        CustomerId = customerId,
                        Timestamp = DateTime.UtcNow,
                        CreditMultipliers = mcMetrics.RawCreditMultipliers,
                        InterestDeltas = mcMetrics.RawInterestDeltas,
                        VaR95 = mcMetrics.VaR95CreditMultiplier,
                        CVaR95 = mcMetrics.ExpectedShortfallCredit,
                        ScenarioSetJson = rawResultJson
                    });
                }
            }
            catch
            {
                action = await rlEngine.DecideAsync(state);
                switch (action.Value)
                {
                    case DecisionAction.LowRisk:
                        creditLimit *= 1.5m * globalCreditMultiplier;
                        interestRate *= 0.8m;
                        break;
                    case DecisionAction.HighRisk:
                        creditLimit *= 0.5m * globalCreditMultiplier;
                        interestRate *= 1.5m;
                        break;
                    case DecisionAction.Reject: creditLimit = 0m; break;
                }
            }
        }
        else
        {
            action = await rlEngine.DecideAsync(state);
            switch (action.Value)
            {
                case DecisionAction.LowRisk:
                    creditLimit *= 1.5m * globalCreditMultiplier;
                    interestRate *= 0.8m;
                    break;
                case DecisionAction.HighRisk:
                    creditLimit *= 0.5m * globalCreditMultiplier;
                    interestRate *= 1.5m;
                    break;
                case DecisionAction.Reject: creditLimit = 0m; break;
            }
        }

        (creditLimit, interestRate) = guardrailEngine.Apply(creditLimit, interestRate, globalState);

        result.CreditLimit = creditLimit;
        result.InterestRate = interestRate;

        if (!isReplay)
        {
            var outcome = new ModelOutcome(customerId, finalPd, prodVersion, false, 0m);
            db.ModelOutcomes.Add(outcome);

            var dbSnapshot = new DecisionSnapshot(
                customerId,
                finalPd,
                prodVersion,
                result.CreditLimit,
                result.InterestRate,
                result.FraudScore
            );

            db.DecisionSnapshots.Add(dbSnapshot);

            db.DecisionOutcomes.Add(new DecisionOutcome
            {
                CustomerId = customerId,
                StateKey = state.ToKey(),
                Action = action ?? DecisionAction.MediumRisk,
                CreditLimit = creditLimit,
                InterestRate = interestRate,
                ModelVersion = prodVersion
            });

            db.ReplaySnapshots.Add(new ReplaySnapshot
            {
                CustomerId = customerId,
                OriginalTimestamp = DateTime.UtcNow,
                FeatureVectorJson = System.Text.Json.JsonSerializer.Serialize(features),
                MacroStateJson = System.Text.Json.JsonSerializer.Serialize(macro),
                PortfolioStateJson = System.Text.Json.JsonSerializer.Serialize(globalState),
                OriginalCreditLimit = result.CreditLimit,
                OriginalInterestRate = result.InterestRate,
                ModelVersion = prodVersion,
                RandomSeed = seed,
                ModelHash = prodVersion, // Usually a SHA of the weights
                FeatureVersion = "1.0",
                ScenarioVersion = "1.0",
            });

            await db.SaveChangesAsync(ct);

            await cache.SetAsync(key, result, TimeSpan.FromMinutes(5), ct);
        }

        return result;
    }
}
