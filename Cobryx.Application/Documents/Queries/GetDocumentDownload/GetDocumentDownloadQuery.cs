using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Documents.Queries.GetDocumentDownload;

public record GetDocumentDownloadQuery(Guid DocumentId) : IRequest<Result<DocumentDownloadDto>>;

public record DocumentDownloadDto(string Url, DateTime ExpiresAtUtc);
