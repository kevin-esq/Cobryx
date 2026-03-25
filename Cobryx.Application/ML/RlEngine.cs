using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.ML;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.ML;

public interface IRlEngine
{
    public Task<DecisionAction> DecideAsync(RlState state);
    public Task UpdateAsync(string stateKey, DecisionAction action, decimal reward);
}

public class RlEngine : IRlEngine
{
    private readonly ICobryxDbContext _db;
    private readonly RlPolicy _policy;

    public RlEngine(ICobryxDbContext db, RlPolicy policy)
    {
        _db = db;
        _policy = policy;
    }

    public async Task<DecisionAction> DecideAsync(RlState state)
    {
        var key = state.ToKey();

        var qValues = await _db.QValues
            .Where(x => x.StateKey == key)
            .ToListAsync();

        return _policy.Select(qValues.Select(x => (x.Action, x.Value)).ToList());
    }

    public async Task UpdateAsync(string stateKey, DecisionAction action, decimal reward)
    {
        var q = await _db.QValues
            .FirstOrDefaultAsync(x => x.StateKey == stateKey && x.Action == action);

        if (q == null)
        {
            _db.QValues.Add(new QValue
            {
                StateKey = stateKey,
                Action = action,
                Value = reward
            });
        }
        else
        {
            q.Value = (q.Value * 0.9m) + (reward * 0.1m);
        }

        await _db.SaveChangesAsync(CancellationToken.None);
    }
}
