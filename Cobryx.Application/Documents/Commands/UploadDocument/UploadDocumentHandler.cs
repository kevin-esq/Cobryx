using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Documents.Services;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Exceptions.Tenants;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Documents.Commands.UploadDocument;

public class UploadDocumentHandler : IRequestHandler<UploadDocumentCommand, Result<UploadDocumentResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentStorage _storage;
    private readonly ITenantProvider _tenantProvider;
    private readonly FileSignatureValidator _validator;
    private readonly IClock _clock;

    public UploadDocumentHandler(
        IUnitOfWork unitOfWork,
        IDocumentStorage storage,
        ITenantProvider tenantProvider,
        FileSignatureValidator validator,
        IClock clock)
    {
        _unitOfWork = unitOfWork;
        _storage = storage;
        _tenantProvider = tenantProvider;
        _validator = validator;
        _clock = clock;
    }

    public async Task<Result<UploadDocumentResult>> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? request.TenantId;
        if (tenantId == Guid.Empty) throw new TenantContextMissingException();

        // 1. Validate file (size, extension, MIME, magic bytes)
        var validation = _validator.Validate(request.FileStream, request.FileName, request.ContentType);
        if (!validation.IsSuccess)
        {
            return Result.Failure<UploadDocumentResult>(validation.Error!);
        }

        // 2. Upload to storage
        request.FileStream.Position = 0;
        var blobPath = await _storage.UploadAsync(request.FileStream, request.FileName, request.ContentType);

        // 3. Create entity with PendingScan
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

        // 4. Persist — OutboxInterceptor captures DocumentUploadedEvent raised in constructor
        var db = (Microsoft.EntityFrameworkCore.DbContext)_unitOfWork;
        await db.Set<DocumentMetadata>().AddAsync(document, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new UploadDocumentResult(document.Id, document.ScanStatus.ToString()));
    }
}
