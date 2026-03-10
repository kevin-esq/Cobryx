using Cobryx.Application.Accounting.Events;

namespace Cobryx.Application.Accounting.Services;

public interface IShadowReplayEngine
{
    Task ProcessEventAsync(LedgerCdcEvent cdcEvent, CancellationToken ct = default);
    Task<long> GetLastSequenceAsync(CancellationToken ct = default);
}
