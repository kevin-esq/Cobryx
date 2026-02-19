using Cobryx.Domain.Common;
using Concordia;

namespace Cobryx.Application.Documents.Queries.GetDocumentStatus;

public record GetDocumentStatusQuery(Guid DocumentId) : IRequest<Result<DocumentStatusDto>>;

public record DocumentStatusDto(
    Guid Id,
    string FileName,
    long FileSize,
    string MimeType,
    string ScanStatus,
    DateTime? ScannedAtUtc
);
