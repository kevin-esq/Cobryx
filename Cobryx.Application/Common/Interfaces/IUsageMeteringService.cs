namespace Cobryx.Application.Common.Interfaces;

public record UsageSnapshot(
    int InvoicesCount,
    int ActiveUsersCount,
    int ActiveLoansCount,
    int MaxInvoices,
    int MaxUsers,
    int MaxLoans);

public interface IUsageMeteringService
{
    Task<UsageSnapshot> GetUsageSnapshotAsync(Guid tenantId, CancellationToken ct = default);

    static string GetCacheKey(Guid tenantId) => $"usage:snapshot:{tenantId}";
}
