using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

using Cobryx.Domain.Decision;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Decision
{
    public class SnapshotStore : ISnapshotStore
    {
        private readonly Channel<ProductionSnapshot> _channel;
        private readonly ILogger<SnapshotStore> _logger;

        public SnapshotStore(ILogger<SnapshotStore> logger)
        {
            _logger = logger;

            var options = new BoundedChannelOptions(5000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            };
            _channel = Channel.CreateBounded<ProductionSnapshot>(options);
        }

        public ChannelReader<ProductionSnapshot> Reader => _channel.Reader;

        public async Task QueueSnapshotAsync(
            Guid customerId,
            string engineVersion,
            string configHash,
            object features,
            object macroState,
            object portfolioState,
            ExecutionTrace trace,
            DecisionResult result,
            bool isFull,
            CancellationToken ct = default)
        {
            try
            {
                var traceJson = JsonSerializer.Serialize(trace);
                var traceHash = ComputeHash(traceJson);

                var hashedCustomerId = ComputeHash(customerId.ToString());

                var snapshot = new ProductionSnapshot(
                    hashedCustomerId,
                    engineVersion,
                    configHash,
                    traceHash,
                    isFull,
                    result.CreditLimit,
                    result.InterestRate,
                    isFull ? JsonSerializer.Serialize(features) : null,
                    isFull ? JsonSerializer.Serialize(macroState) : null,
                    isFull ? JsonSerializer.Serialize(portfolioState) : null,
                    isFull ? traceJson : null,
                    isFull ? JsonSerializer.Serialize(result) : null
                );

                if (!_channel.Writer.TryWrite(snapshot))
                {
                    _logger.LogWarning("Snapshot channel is full. Dropping snapshot for customer {HashedId}",
                        hashedCustomerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue snapshot for anonymized customer {CustomerId}", customerId);
            }

            await Task.CompletedTask;
        }

        private static string ComputeHash(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
