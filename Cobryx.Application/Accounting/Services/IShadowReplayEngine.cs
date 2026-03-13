using Cobryx.Application.Accounting.Events;

namespace Cobryx.Application.Accounting.Services;

public interface IShadowReplayEngine
{
    public Task ProcessEventAsync(LedgerCdcEvent cdcEvent, CancellationToken ct = default);
    public Task<long> GetLastSequenceAsync(CancellationToken ct = default);
}
