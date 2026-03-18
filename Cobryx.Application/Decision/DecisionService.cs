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

    public DecisionService(
        DecisionEngine engine,
        ICacheService cache,
        ICobryxDbContext db,
        Cobryx.Application.ML.IFeatureStore featureStore,
        Cobryx.Application.ML.MlClient mlClient)
    {
        _engine = engine;
        _cache = cache;
        _db = db;
        _featureStore = featureStore;
        _mlClient = mlClient;
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
        decimal mlPd;
        string modelVersion;

        try
        {
            (mlPd, modelVersion) = await _mlClient.PredictAsync(features!);
        }
        catch
        {
            mlPd = heuristicPd;
            modelVersion = "fallback-heuristic";
        }

        var finalPd = (heuristicPd * 0.3m) + (mlPd * 0.7m);

        ctx.Credit.ProbabilityOfDefault = finalPd;
        ctx.Pricing.ProbabilityOfDefault = finalPd;

        var result = _engine.Evaluate(ctx);

        // persist outcome logic mapping for ML retraining feedback loop
        var outcome = new Cobryx.Domain.ML.ModelOutcome(customerId, finalPd, modelVersion, false, 0m);
        _db.ModelOutcomes.Add(outcome);

        // persist snapshot
        var snapshot = new DecisionSnapshot(
            customerId,
            finalPd,
            modelVersion,
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
