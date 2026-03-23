using System.Text.Json;
using Cobryx.Application.Decision;
using Cobryx.Domain.Decision;
using Cobryx.Domain.ML;

namespace Cobryx.Application.ML;

public class ReplayEngine(DecisionService decisionService)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<ReplaySnapshot> ReplayAsync(ReplaySnapshot snapshot)
    {
        var features = JsonSerializer.Deserialize<FeatureVector>(snapshot.FeatureVectorJson, JsonOptions);
        var macro = JsonSerializer.Deserialize<MacroState>(snapshot.MacroStateJson, JsonOptions);
        var portfolio = JsonSerializer.Deserialize<PortfolioState>(snapshot.PortfolioStateJson, JsonOptions);

        var ctx = new DecisionContext
        {
            Credit = new CreditContext { ProbabilityOfDefault = 0.05m },
            Pricing = new PricingContext { ProbabilityOfDefault = 0.05m }
        };

        var result = await decisionService.EvaluateAsync(
            snapshot.CustomerId, 
            ctx, 
            overrideFeatures: features, 
            overrideMacro: macro, 
            overridePortfolio: portfolio,
            isReplay: true);

        snapshot.ReplayedCreditLimit = result.CreditLimit;
        snapshot.ReplayedInterestRate = result.InterestRate;

        snapshot.DeltaCredit = result.CreditLimit - snapshot.OriginalCreditLimit;
        snapshot.DeltaInterest = result.InterestRate - snapshot.OriginalInterestRate;
        snapshot.ReplayModelVersion = "Current";

        return snapshot;
    }

    public async Task<List<ReplaySnapshot>> ReplayBatchAsync(List<ReplaySnapshot> snapshots)
    {
        var tasks = snapshots.Select(ReplayAsync);
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
}
