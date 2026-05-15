namespace Cobryx.Application.Common.Interfaces;

public interface ILedgerAnchorService
{
    public Task TriggerAnchoringIfRequiredAsync(Guid tenantId, string currentHash, long sequence, decimal amount, object request, CancellationToken ct = default);
}
