namespace Cobryx.Application.Common.Interfaces;

public interface ICollectionsPriorityStore
{
    public Task<List<PriorityCaseEntry>> GetTopPriorityCasesAsync(Guid tenantId, int limit, CancellationToken cancellationToken = default);
}

public record PriorityCaseEntry(string LoanId, double PriorityScore, object? Metadata);
