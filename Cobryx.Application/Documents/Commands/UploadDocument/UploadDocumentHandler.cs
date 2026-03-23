using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Documents.Services;
using Cobryx.Domain.Exceptions.Tenants;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Documents.Commands.UploadDocument;

public class UploadDocumentHandler(
    IUnitOfWork unitOfWork,
    IDocumentStorage storage,
    ITenantProvider tenantProvider,
    FileSignatureValidator validator,
    IClock clock) : IRequestHandler<UploadDocumentCommand, Result<UploadDocumentResult>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDocumentStorage _storage = storage;
    private readonly ITenantProvider _tenantProvider = tenantProvider;
    private readonly FileSignatureValidator _validator = validator;
    private readonly IClock _clock = clock;

    public async Task<Result<UploadDocumentResult>> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? request.TenantId;
        if (tenantId == Guid.Empty)
            throw new TenantContextMissingException();

        var validation = _validator.Validate(request.FileStream, request.FileName, request.ContentType);
        if (!validation.IsSuccess)
        {
            return Result.Failure<UploadDocumentResult>(validation.Error!);
        }

        request.FileStream.Position = 0;
        var blobPath = await _storage.UploadAsync(request.FileStream, request.FileName, request.ContentType);

        var document = new DocumentMetadata(
            tenantId,
            request.EntityId,
            request.EntityType,
            request.FileName,
            blobPath,
            request.FileStream.Length,
            request.ContentType,
            request.UploadedBy,
            _clock.UtcNow);

        var db = (Microsoft.EntityFrameworkCore.DbContext)_unitOfWork;
        await db.Set<DocumentMetadata>().AddAsync(document, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new UploadDocumentResult(document.Id, document.ScanStatus.ToString()));
    }
}
