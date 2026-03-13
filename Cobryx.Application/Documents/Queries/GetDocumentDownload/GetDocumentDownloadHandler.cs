using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Domain.Shared.Enums;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Documents.Queries.GetDocumentDownload;

public class GetDocumentDownloadHandler(IUnitOfWork unitOfWork, ITenantProvider tenantProvider, IDocumentStorage storage, IClock clock) : IRequestHandler<GetDocumentDownloadQuery, Result<DocumentDownloadDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ITenantProvider _tenantProvider = tenantProvider;
    private readonly IDocumentStorage _storage = storage;
    private readonly IClock _clock = clock;
    private static readonly TimeSpan UrlExpiry = TimeSpan.FromMinutes(15);

    public async Task<Result<DocumentDownloadDto>> Handle(GetDocumentDownloadQuery request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var db = (DbContext)_unitOfWork;

        var doc = await db.Set<DocumentMetadata>()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.TenantId == tenantId, ct);

        if (doc == null)
            return Result.Failure<DocumentDownloadDto>(DomainErrorCode.Documents.NotFound);

        // Hard block: never serve unscanned or infected files
        if (doc.ScanStatus != ScanStatus.Clean)
            return Result.Failure<DocumentDownloadDto>(DomainErrorCode.Documents.ScanInProgress);

        var url = await _storage.GetPreSignedUrlAsync(doc.BlobPath, UrlExpiry);
        var expiresAt = _clock.UtcNow.Add(UrlExpiry);

        return Result.Success(new DocumentDownloadDto(url, expiresAt));
    }
}
