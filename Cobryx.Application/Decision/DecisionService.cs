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

    public DecisionService(
        DecisionEngine engine,
        ICacheService cache,
        ICobryxDbContext db,
        Cobryx.Application.ML.IFeatureStore featureStore,
        Cobryx.Application.ML.MlClient mlClient,
        Cobryx.Application.ML.ModelRouter router,
        Cobryx.Application.ML.EnsembleService ensemble)
    {
        _engine = engine;
        _cache = cache;
        _db = db;
        _featureStore = featureStore;
        _mlClient = mlClient;
        _router = router;
        _ensemble = ensemble;
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
        await _db.SaveChangesAsync(ct);

        // cache
        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(5), ct);

        return result;
    }
}
