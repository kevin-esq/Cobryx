using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision;

public class DecisionService
{
    private readonly DecisionEngine _engine;
    private readonly ICacheService _cache;
    private readonly ICobryxDbContext _db;

    public DecisionService(
        DecisionEngine engine,
        ICacheService cache,
        ICobryxDbContext db)
    {
        _engine = engine;
        _cache = cache;
        _db = db;
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

        var result = _engine.Evaluate(ctx);

        // persist snapshot
        var snapshot = new DecisionSnapshot(
            customerId,
            ctx.Credit.ProbabilityOfDefault,
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
