using Cobryx.Domain.Accounting;

namespace Cobryx.Application.Common.Interfaces;

/// <summary>
/// Provides cryptographic hashing for ledger transactions to ensure immutability and chain integrity.
/// </summary>
public interface ILedgerHasher
{
    /// <summary>
    /// Computes the deterministic SHA256 hash for a transaction based on the V1 payload contract.
    /// Payload: previousHash|txId|tenantId|entriesPayload|timestamp|correlationId
    /// </summary>
    public string ComputeHash(LedgerTransaction transaction, string previousHash);

    /// <summary>
    /// Computes the canonical entries payload for a transaction.
    /// Payload: OrderedBy(AccountId).ThenBy(Id) -> AccountId:Debit:Credit
    /// </summary>
    public string ComputeEntriesPayload(IEnumerable<LedgerEntry> entries);
}
