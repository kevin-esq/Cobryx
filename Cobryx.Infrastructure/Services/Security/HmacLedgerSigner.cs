using System.Security.Cryptography;
using System.Text;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting.Models;

namespace Cobryx.Infrastructure.Services.Security
{
    public sealed class HmacLedgerSigner : ILedgerSigner
    {
        private readonly byte[] _key;

        public HmacLedgerSigner()
        {
            // PROD: Fetch from secure configuration/key vault
            _key = Encoding.UTF8.GetBytes("tier-0-audit-secret-key-simulated");
        }

        public string Sign(LedgerAnchor anchor, Guid tenantId)
        {
            // Payload for signature: TenantId|Sequence|Hash|PrevAnchorHash|Date
            var payload = $"{tenantId}|{anchor.Sequence}|{anchor.Hash}|{anchor.PreviousAnchorHash}|{anchor.AnchoredAtUtc:O}";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(_key);
            var hash = hmac.ComputeHash(payloadBytes);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
