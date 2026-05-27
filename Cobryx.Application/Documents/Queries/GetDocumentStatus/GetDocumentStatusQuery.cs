using Cobryx.Domain.Shared;
using Cobryx.Application.Common.Interfaces;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Documents.Queries.GetDocumentStatus;

[TenantScoped]
public record GetDocumentStatusQuery(Guid DocumentId) : IRequest<Result<DocumentStatusDto>>, IRequiresTenant;

public record DocumentStatusDto(
    Guid Id,
    string FileName,
    long FileSize,
    string MimeType,
    string ScanStatus,
    DateTime? ScannedAtUtc
);
