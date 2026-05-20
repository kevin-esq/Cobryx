using Cobryx.Domain.Accounting.Models;

namespace Cobryx.Application.Common.Interfaces;

public interface ILedgerAnchorStore
{
    public Task AppendAsync(LedgerAnchor anchor, CancellationToken ct = default);
    public LedgerAnchor? GetLatest(Guid tenantId, CancellationToken ct = default);
}
