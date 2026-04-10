using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Documents.Commands.UploadDocument;

// * TenantId parameter is IGNORED by handler for security.
// Tenant context is resolved via ITenantProvider.
// This parameter is deprecated and will be removed in future refactor.
[TenantScoped]
public record UploadDocumentCommand(
    Guid TenantId,
    Guid EntityId,
    string EntityType,
    Stream FileStream,
    string FileName,
    string ContentType,
    Guid UploadedBy
) : IRequest<Result<UploadDocumentResult>>, IRequiresTenant;

public record UploadDocumentResult(Guid DocumentId, string ScanStatus);
