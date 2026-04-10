using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Documents.Services;
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
    public async Task<Result<UploadDocumentResult>> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure<UploadDocumentResult>(DomainErrorCode.Tenant.ContextMissing);

        Result validation = validator.Validate(request.FileStream, request.FileName, request.ContentType);
        if (!validation.IsSuccess)
        {
            return Result.Failure<UploadDocumentResult>(validation.Error!);
        }

        request.FileStream.Position = 0;
        var blobPath = await storage.UploadAsync(request.FileStream, request.FileName, request.ContentType);

        var document = new DocumentMetadata(
            tenantId.Value,
            request.EntityId,
            request.EntityType,
            request.FileName,
            blobPath,
            request.FileStream.Length,
            request.ContentType,
            request.UploadedBy,
            clock.UtcNow);

        var db = (Microsoft.EntityFrameworkCore.DbContext)unitOfWork;
        await db.Set<DocumentMetadata>().AddAsync(document, ct);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new UploadDocumentResult(document.Id, document.ScanStatus.ToString()));
    }
}
