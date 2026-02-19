using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Documents.Queries.GetDocumentDownload;

public class GetDocumentDownloadHandler : IRequestHandler<GetDocumentDownloadQuery, Result<DocumentDownloadDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDocumentStorage _storage;
    private readonly IClock _clock;

    private static readonly TimeSpan UrlExpiry = TimeSpan.FromMinutes(15);

    public GetDocumentDownloadHandler(IUnitOfWork unitOfWork, ITenantProvider tenantProvider, IDocumentStorage storage, IClock clock)
    {
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _storage = storage;
        _clock = clock;
    }

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
