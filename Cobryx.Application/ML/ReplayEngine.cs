using System.Text.Json;

using Cobryx.Application.Decision;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML.Models;
using Cobryx.Domain.Decision;
using Cobryx.Domain.ML;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.ML
{
    public class ReplayEngine(DecisionService decisionService, ILogger<ReplayEngine> logger)
        : Decision.Interfaces.IReplayEngine
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public async Task<ReplayResult> ReplayAsync(IReplayInput input)
        {
            var features = !string.IsNullOrWhiteSpace(input.FeatureVectorJson)
                ? JsonSerializer.Deserialize<FeatureVector>(input.FeatureVectorJson, _jsonOptions)
                : new FeatureVector();

            var macro = !string.IsNullOrWhiteSpace(input.MacroStateJson)
                ? JsonSerializer.Deserialize<MacroState>(input.MacroStateJson, _jsonOptions)
                : new MacroState();

            var portfolio = !string.IsNullOrWhiteSpace(input.PortfolioStateJson)
                ? JsonSerializer.Deserialize<PortfolioState>(input.PortfolioStateJson, _jsonOptions)
                : new PortfolioState();

            var ctx = string.IsNullOrWhiteSpace(input.DecisionContextJson)
                ? new DecisionContext { Credit = new(), Pricing = new(), Fraud = new() }
                : JsonSerializer.Deserialize<DecisionContext>(input.DecisionContextJson, _jsonOptions);

            var result = await decisionService.EvaluateAsync(
                input.CustomerId,
                ctx!,
                overrideFeatures: features,
                overrideMacro: macro,
                overridePortfolio: portfolio,
                overrideSeed: input.RandomSeed,
                isReplay: true);

            var originalTrace = !string.IsNullOrWhiteSpace(input.ExecutionTraceJson)
                ? JsonSerializer.Deserialize<ExecutionTrace>(input.ExecutionTraceJson, _jsonOptions)
                : null;

            var replayedTrace = result.Trace;

            var originalHash = originalTrace?.GetTraceHash(input.EngineVersion) ?? "";
            var replayedHash = replayedTrace.GetTraceHash(EngineMetadata.EngineVersion);

            var replayResult = new ReplayResult
            {
                IsDeterministic = originalHash == replayedHash,
                DeltaCreditLimit = result.CreditLimit - input.OriginalLimit,
                DeltaInterestRate = result.InterestRate - input.OriginalRate,
                OriginalEngineVersion = input.EngineVersion,
                ReplayedEngineVersion = EngineMetadata.EngineVersion,
                OriginalTraceHash = originalHash,
                ReplayedTraceHash = replayedHash,
                TraceDriftDetected = originalHash != replayedHash
            };

            if (replayResult.TraceDriftDetected)
            {
                logger.LogWarning(
                    "Trace drift detected for Source {SourceId}. Hash {OriginalHash} vs {ReplayedHash}",
                    input.SourceId, originalHash, replayedHash);
                replayResult.Diff = AnalyzeDiff(originalTrace, replayedTrace);
            }
            else if (!replayResult.IsDeterministic)
            {
                logger.LogWarning("Output drift detected for Source {SourceId} despite matching trace hash.",
                    input.SourceId);
            }

            return replayResult;
        }

        private static TraceDiff AnalyzeDiff(ExecutionTrace? original, ExecutionTrace? replayed)
        {
            var diff = new TraceDiff();
            if (original == null || replayed == null)
            {
                return diff;
            }

            var count = Math.Max(original.Steps.Count, replayed.Steps.Count);
            for (var i = 0; i < count; i++)
            {
                var origStep = i < original.Steps.Count ? original.Steps[i] : null;
                var replStep = i < replayed.Steps.Count ? replayed.Steps[i] : null;

                if (origStep?.OutputValue != replStep?.OutputValue || origStep?.Description != replStep?.Description)
                {
                    diff.Mismatches.Add(new StepDiff
                    {
                        StepName = origStep?.StepName ?? replStep?.StepName ?? $"Step {i}",
                        OriginalOutput = origStep?.OutputValue ?? 0,
                        ReplayedOutput = replStep?.OutputValue ?? 0,
                        DescriptionMismatch = origStep?.Description != replStep?.Description
                            ? $"Orig: {origStep?.Description} | Repl: {replStep?.Description}"
                            : ""
                    });
                }
            }

            return diff;
        }

        public async Task<List<ReplayResult>> ReplayBatchAsync(List<IReplayInput> inputs)
        {
            var tasks = inputs.Select(ReplayAsync);
            var res = await Task.WhenAll(tasks);
            return [.. res];
        }
    }
}
