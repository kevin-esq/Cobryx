using System.Security.Cryptography;
using System.Text;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting.Models;

using Newtonsoft.Json;

namespace Cobryx.Infrastructure.Services.Security
{
    public sealed class ChainedFileAnchorStore : ILedgerAnchorStore
    {
        private readonly string _basePath;
        private static readonly object _lock = new();

        public ChainedFileAnchorStore()
        {
            _basePath = Path.Combine(Directory.GetCurrentDirectory(), "Cobryx.Infrastructure", "InternalStorage", "Anchors");
            if (!Directory.Exists(_basePath))
            {
                _ = Directory.CreateDirectory(_basePath);
            }
        }

        public async Task AppendAsync(LedgerAnchor anchor, CancellationToken ct = default)
        {
            var filePath = GetFilePath(anchor.TenantId);
            var json = JsonConvert.SerializeObject(anchor);

            // Calculate link to the PREVIOUS LINE in the file for tamper-evidence
            var prevLineHash = "genesis";

            lock (_lock)
            {
                if (File.Exists(filePath))
                {
                    var lastLine = File.ReadLines(filePath).LastOrDefault();
                    if (!string.IsNullOrEmpty(lastLine))
                    {
                        var parts = lastLine.Split('|');
                        if (parts.Length == 3)
                        {
                            prevLineHash = parts[2]; // The hash of the previous record
                        }
                    }
                }

                var currentLinePayload = $"{json}|{prevLineHash}";
                var currentLineHash = ComputeSha256(currentLinePayload);

                // Format: LEN|PAYLOAD|CHAIN_HASH
                var lineToAppend = $"{currentLinePayload.Length}|{currentLinePayload}|{currentLineHash}";

                File.AppendAllLines(filePath, [lineToAppend]);
            }

            await Task.CompletedTask;
        }

        public LedgerAnchor? GetLatest(Guid tenantId, CancellationToken ct = default)
        {
            var filePath = GetFilePath(tenantId);
            if (!File.Exists(filePath))
            {
                return null;
            }

            lock (_lock)
            {
                var lastLine = File.ReadLines(filePath).LastOrDefault();
                if (string.IsNullOrEmpty(lastLine))
                {
                    return null;
                }

                var parts = lastLine.Split('|');
                if (parts.Length < 2)
                {
                    return null;
                }

                // Index 1 is the Payload (JSON|PREV_HASH)
                var payloadParts = parts[1].Split('|');
                return payloadParts.Length < 1 ? null : JsonConvert.DeserializeObject<LedgerAnchor>(payloadParts[0]);
            }
        }

        private string GetFilePath(Guid tenantId) => Path.Combine(_basePath, $"{tenantId}.log");

        private static string ComputeSha256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
