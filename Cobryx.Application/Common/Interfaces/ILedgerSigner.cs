using Cobryx.Domain.Accounting.Models;

namespace Cobryx.Application.Common.Interfaces;

public interface ILedgerSigner
{
    public string Sign(LedgerAnchor anchor, Guid tenantId);
}
