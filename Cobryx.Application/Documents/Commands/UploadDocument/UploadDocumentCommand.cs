using Cobryx.Domain.Common;
using Concordia;

namespace Cobryx.Application.Documents.Commands.UploadDocument;

public record UploadDocumentCommand(
    Guid TenantId,
    Guid EntityId,
    string EntityType,
    Stream FileStream,
    string FileName,
    string ContentType,
    Guid UploadedBy
) : IRequest<Result<UploadDocumentResult>>;

public record UploadDocumentResult(Guid DocumentId, string ScanStatus);
