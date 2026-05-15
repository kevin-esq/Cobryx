using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Accounting.Models
{
    /// <summary>
    /// Represents a point-in-time proof of the ledger state, anchored to an external immutable store.
    /// Anchors are chained together via PreviousAnchorHash to prevent historical substitution attacks.
    /// </summary>
    public record LedgerAnchor(
        Guid TenantId,
        long Sequence,
        string Hash,
        string PreviousAnchorHash,
        DateTime AnchoredAtUtc,
        string Signature) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return TenantId;
            yield return Sequence;
            yield return Hash;
            yield return PreviousAnchorHash;
            yield return AnchoredAtUtc;
            yield return Signature;
        }
    }
}
