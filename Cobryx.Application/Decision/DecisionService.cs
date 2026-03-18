using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision;

public class DecisionService
{
    private readonly DecisionEngine _engine;
    private readonly ICacheService _cache;
    private readonly ICobryxDbContext _db;
    private readonly Cobryx.Application.ML.IFeatureStore _featureStore;
    private readonly Cobryx.Application.ML.MlClient _mlClient;

    private readonly Cobryx.Application.ML.ModelRouter _router;
    private readonly Cobryx.Application.ML.EnsembleService _ensemble;
    private readonly Cobryx.Application.ML.IRlEngine _rlEngine;

    public DecisionService(
        DecisionEngine engine,
        ICacheService cache,
        ICobryxDbContext db,
        Cobryx.Application.ML.IFeatureStore featureStore,
        Cobryx.Application.ML.MlClient mlClient,
        Cobryx.Application.ML.ModelRouter router,
        Cobryx.Application.ML.EnsembleService ensemble,
        Cobryx.Application.ML.IRlEngine rlEngine)
    {
        _engine = engine;
        _cache = cache;
        _db = db;
        _featureStore = featureStore;
        _mlClient = mlClient;
        _router = router;
        _ensemble = ensemble;
        _rlEngine = rlEngine;
    }

    public async Task<DecisionResult> EvaluateAsync(
        Guid customerId,
        DecisionContext ctx,
        CancellationToken ct = default)
    {
        var key = $"decision:{customerId}";

        var cached = await _cache.GetAsync<DecisionResult>(key, ct);

        if (cached != null)
            return cached;

        // ML INFERENCE PIPELINE
        var features = await _featureStore.GetAsync(customerId);

        var heuristicPd = ctx.Credit.ProbabilityOfDefault;
        decimal prodPd;
        string prodVersion;

        try
        {
            (prodPd, prodVersion) = await _mlClient.PredictAsync(features!, "xgb_v1");
        }
        catch
        {
            prodPd = heuristicPd;
            prodVersion = "fallback-heuristic";
        }

        decimal shadowPd = prodPd;
        string shadowVersion = "none";

        if (_router.ShouldRunShadow())
        {
            try
            {
                (shadowPd, shadowVersion) = await _mlClient.PredictAsync(features!, "xgb_v2");

                _db.ShadowPredictions.Add(new Cobryx.Domain.ML.ShadowPrediction
                {
                    CustomerId = customerId,
                    ProductionPd = prodPd,
                    ShadowPd = shadowPd,
                    ProductionModelVersion = prodVersion,
                    ShadowModelVersion = shadowVersion,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch { }
        }

        var finalPd = _ensemble.Combine(heuristicPd, prodPd, shadowPd);

        ctx.Credit.ProbabilityOfDefault = finalPd;
        ctx.Pricing.ProbabilityOfDefault = finalPd;

        var result = _engine.Evaluate(ctx);

        var state = new Cobryx.Domain.ML.RlState
        {
            PdBucket = Math.Round(finalPd, 1),
            UtilizationBucket = Math.Round(features!.Utilization, 1),
            BehaviorBucket = Math.Round(features!.BehaviorScore, 1)
        };

        var action = await _rlEngine.DecideAsync(state);

        var creditLimit = result.CreditLimit;
        var interestRate = result.InterestRate;

        switch (action)
        {
            case Cobryx.Domain.ML.DecisionAction.LowRisk:
                creditLimit *= 1.5m;
                interestRate *= 0.8m;
                break;
            case Cobryx.Domain.ML.DecisionAction.HighRisk:
                creditLimit *= 0.5m;
                interestRate *= 1.5m;
                break;
            case Cobryx.Domain.ML.DecisionAction.Reject:
                creditLimit = 0m;
                break;
        }

        result.CreditLimit = creditLimit;
        result.InterestRate = interestRate;

        // persist outcome logic mapping for ML retraining feedback loop
        var outcome = new Cobryx.Domain.ML.ModelOutcome(customerId, finalPd, prodVersion, false, 0m);
        _db.ModelOutcomes.Add(outcome);

        // persist snapshot
        var snapshot = new DecisionSnapshot(
            customerId,
            finalPd,
            prodVersion,
            result.CreditLimit,
            result.InterestRate,
            result.FraudScore
        );

        _db.DecisionSnapshots.Add(snapshot);

        _db.DecisionOutcomes.Add(new Cobryx.Domain.ML.DecisionOutcome
        {
            CustomerId = customerId,
            StateKey = state.ToKey(),
            Action = action,
            CreditLimit = creditLimit,
            InterestRate = interestRate,
            ModelVersion = prodVersion
        });

        await _db.SaveChangesAsync(ct);

        // cache
        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(5), ct);

        return result;
    }
}
