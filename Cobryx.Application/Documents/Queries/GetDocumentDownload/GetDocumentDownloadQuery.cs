using Cobryx.Domain.Shared;
using Cobryx.Application.Common.Interfaces;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Documents.Queries.GetDocumentDownload;

[TenantScoped]
public record GetDocumentDownloadQuery(Guid DocumentId) : IRequest<Result<DocumentDownloadDto>>, IRequiresTenant;

public record DocumentDownloadDto(string Url, DateTime ExpiresAtUtc);
