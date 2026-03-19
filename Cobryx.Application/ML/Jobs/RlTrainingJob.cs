using Cobryx.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.ML.Jobs;

public class RlTrainingJob
{
    private readonly ICobryxDbContext _db;
    private readonly IRlEngine _engine;

    public RlTrainingJob(ICobryxDbContext db, IRlEngine engine)
    {
        _db = db;
        _engine = engine;
    }

    public async Task RunAsync()
    {
        var outcomes = await _db.DecisionOutcomes
            .Where(x => x.Reward == 0)
            .ToListAsync();

        foreach (var o in outcomes)
        {
            var reward = (o.AmountRecovered) - (o.Defaulted ? o.CreditLimit : 0);

            await _engine.UpdateAsync(o.StateKey, o.Action, reward);

            o.Reward = reward;
        }

        await _db.SaveChangesAsync(CancellationToken.None);
    }
}
