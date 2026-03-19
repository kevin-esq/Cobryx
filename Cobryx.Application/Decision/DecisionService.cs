using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Decision;
using Cobryx.Domain.ML;

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
    ML.PpoClient ppoClient,
    ML.PortfolioEngine portfolioEngine,
    ML.IPortfolioFeatureStore portfolioStore,
    ML.IMacroFeatureStore macroStore)
{
    public async Task<DecisionResult> EvaluateAsync(
        Guid customerId,
        DecisionContext ctx,
        CancellationToken ct = default)
    {
        var key = $"decision:{customerId}";

        var cached = await cache.GetAsync<DecisionResult>(key, ct);

        if (cached != null)
            return cached;

        // ML INFERENCE PIPELINE
        var features = await featureStore.GetAsync(customerId);

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

        var globalState = await portfolioStore.GetGlobalStateAsync();
        var macro = await macroStore.GetAsync();
        var limits = await cache.GetAsync<PortfolioLimits>("portfolio:limits", ct) ??
                     new PortfolioLimits { MaxExposure = 10000000m };
        decimal globalCreditMultiplier = 1.0m;
        try
        {
            var portfolioAction = await portfolioEngine.OptimizeAsync(globalState);
            globalCreditMultiplier = portfolioAction.CreditMultiplier;
        }
        catch (Exception) { /* ignored */ }

        // 17 Audit: Guardrails FIRST (Monetary Tightening)
        if (macro.InterestRate > 0.15m || macro.Inflation > 0.10m)
        {
            return new DecisionResult { Approved = false, CreditLimit = 0m, InterestRate = 0m };
        }

        if (router.UsePpo())
        {
            try
            {
                var payload = new
                {
                    features = new
                    {
                        utilization = features.Utilization,
                        paymentDelay = features.PaymentDelay,
                        behaviorScore = features.BehaviorScore,
                        dpdTrend = features.DpdTrend,
                        outstanding = features.Outstanding
                    },
                    global_state = globalState,
                    macro = new
                    {
                        interestRate = macro.InterestRate,
                        inflation = macro.Inflation,
                        creditSpread = macro.CreditSpread,
                        volatility = macro.MarketVolatility,
                        regime = macro.Regime,
                        inflationTMinus1 = macro.InflationTMinus1,
                        inflationTMinus2 = macro.InflationTMinus2,
                        rateTrend = macro.RateTrend,
                        timeToMaturity = 12m
                    },
                    model = macro.Country == "MX" ? "ppo_mx" : "ppo_us"
                };

                var combined = await ppoClient.DecideCombinedAsync(payload);
                var ppo = combined.Local;
                globalCreditMultiplier = combined.Portfolio.CreditMultiplier;
                var globalRiskTolerance = combined.Portfolio.RiskTolerance;

                // 1. Global controla el presupuesto
                creditLimit *= globalCreditMultiplier;

                // 2. Local ajusta dentro del presupuesto
                creditLimit *= ppo.CreditMultiplier;
                interestRate += ppo.InterestDelta;

                if (finalPd > globalRiskTolerance)
                {
                    creditLimit *= 0.3m;
                }

                creditLimit = Math.Clamp(creditLimit, 0m, 200000m);
                interestRate = Math.Clamp(interestRate, 0.05m, 0.45m);

                db.Experiences.Add(new Experience
                {
                    CustomerId = customerId,
                    StateJson = System.Text.Json.JsonSerializer.Serialize(features),
                    CreditMultiplier = ppo.CreditMultiplier,
                    InterestDelta = ppo.InterestDelta,
                    LogProb = ppo.LogProb,
                    Value = ppo.Value,
                    Reward = 0m
                });
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

        // GUARDRAILS DEL PORTAFOLIO MULTI-AGENTE (Hard Blocks Override)
        if (globalState.TotalExposure > limits.MaxExposure)
            creditLimit = 0m;

        if (globalState.AvailableLiquidity < 100000m)
            creditLimit *= 0.1m;

        result.CreditLimit = creditLimit;
        result.InterestRate = interestRate;

        // persist outcome logic mapping for ML retraining feedback loop
        var outcome = new ModelOutcome(customerId, finalPd, prodVersion, false, 0m);
        db.ModelOutcomes.Add(outcome);

        // persist snapshot
        var snapshot = new DecisionSnapshot(
            customerId,
            finalPd,
            prodVersion,
            result.CreditLimit,
            result.InterestRate,
            result.FraudScore
        );

        db.DecisionSnapshots.Add(snapshot);

        db.DecisionOutcomes.Add(new DecisionOutcome
        {
            CustomerId = customerId,
            StateKey = state.ToKey(),
            Action = action ?? DecisionAction.MediumRisk,
            CreditLimit = creditLimit,
            InterestRate = interestRate,
            ModelVersion = prodVersion
        });

        await db.SaveChangesAsync(ct);

        // cache
        await cache.SetAsync(key, result, TimeSpan.FromMinutes(5), ct);

        return result;
    }
}
