using Cobryx.Domain.Shared;

namespace Cobryx.Application.Common.Exceptions
{
    /// <summary>
    /// Exception thrown when a financial operation is attempted on a tenant currently in Safe Mode.
    /// Maps to 409 Conflict at the API layer.
    /// </summary>
    public class FinancialSafeModeException(Guid tenantId, string reason) : CobryxException(new Dictionary<string, object>
            {
                { "tenantId", tenantId },
                { "reason", reason }
            })
    {
        public override DomainErrorCode ErrorCode => DomainErrorCode.Accounting.FinancialSafeModeActive;

        public Guid TenantId { get; } = tenantId;
        public string Reason { get; } = reason;
    }
}
