using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Documents.Queries.GetDocumentStatus;

public class GetDocumentStatusHandler : IRequestHandler<GetDocumentStatusQuery, Result<DocumentStatusDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public GetDocumentStatusHandler(IUnitOfWork unitOfWork, ITenantProvider tenantProvider)
    {
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<DocumentStatusDto>> Handle(GetDocumentStatusQuery request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var db = (DbContext)_unitOfWork;

        var doc = await db.Set<DocumentMetadata>()
            .Where(d => d.Id == request.DocumentId && d.TenantId == tenantId)
            .Select(d => new DocumentStatusDto(
                d.Id,
                d.FileName,
                d.FileSize,
                d.MimeType,
                d.ScanStatus.ToString(),
                d.ScannedAtUtc))
            .FirstOrDefaultAsync(ct);

        if (doc == null)
            return Result.Failure<DocumentStatusDto>(DomainErrorCode.Documents.NotFound);

        return Result.Success(doc);
    }
}
