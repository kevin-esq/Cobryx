using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;

namespace Cobryx.Infrastructure.Services.Security
{
    /// <summary>
    /// Production-ready implementation of the Ledger Hash Chain V1.1 contract.
    /// Ensures deterministic canonical hashing with SHA256 and Length-Prefixing to prevent concatenation ambiguity.
    /// </summary>
    public sealed class LedgerHasher : ILedgerHasher
    {
        private const string NullCorrelationId = "null";
        private const string Version = "v1.1";

        public string ComputeHash(LedgerTransaction transaction, string previousHash)
        {
            var entriesPayload = ComputeEntriesPayload(transaction.Entries);
            var timestamp = transaction.EffectiveDate.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            var correlationId = transaction.CorrelationId?.ToString() ?? NullCorrelationId;

            // V1.1 Contract: Version|len(prevHash):prevHash|len(txId):txId...
            var rawPayload = new StringBuilder()
                .Append(Version).Append('|')
                .Append(Prefix(previousHash)).Append('|')
                .Append(Prefix(transaction.Id.ToString())).Append('|')
                .Append(Prefix(transaction.TenantId.ToString())).Append('|')
                .Append(Prefix(entriesPayload)).Append('|')
                .Append(Prefix(timestamp)).Append('|')
                .Append(Prefix(correlationId))
                .ToString();

            return ComputeSha256(rawPayload);
        }

        private static string Prefix(string value) => $"{value.Length}:{value}";

        public string ComputeEntriesPayload(IEnumerable<LedgerEntry> entries)
        {
            // Canonical Order: AccountId ASC, then BaseEntity.Id ASC
            // Normalization: F2 format with InvariantCulture
            var prioritizedEntries = entries
                .OrderBy(static e => e.AccountId)
                .ThenBy(static e => e.Id)
                .Select(static e => $"{e.AccountId.ToString().Length}:{e.AccountId}:{e.Debit.ToString("F2", CultureInfo.InvariantCulture)}:{e.Credit.ToString("F2", CultureInfo.InvariantCulture)}");

            return string.Join("|", prioritizedEntries);
        }

        private static string ComputeSha256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
