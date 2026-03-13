using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Events.Documents;

public record DocumentUploadedEvent(
    Guid DocumentId,
    Guid TenantId,
    string FileName,
    string BlobPath,
    DateTime OccurredOn
) : IDomainEvent;
