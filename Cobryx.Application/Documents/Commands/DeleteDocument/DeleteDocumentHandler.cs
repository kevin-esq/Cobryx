using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Documents.Commands.DeleteDocument;

public class DeleteDocumentHandler(
    IUnitOfWork unitOfWork,
    IDocumentStorage storage,
    ITenantProvider tenantProvider,
    ILogger<DeleteDocumentHandler> logger) : IRequestHandler<DeleteDocumentCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDocumentStorage _storage = storage;
    private readonly ITenantProvider _tenantProvider = tenantProvider;
    private readonly ILogger<DeleteDocumentHandler> _logger = logger;

    public async Task<Result> Handle(DeleteDocumentCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == Guid.Empty)
            return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        var db = (DbContext)_unitOfWork;
        var doc = await db.Set<DocumentMetadata>()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.TenantId == tenantId, ct);

        if (doc == null)
            return Result.Failure(DomainErrorCode.Documents.NotFound);

        try
        {
            await _storage.DeleteAsync(doc.BlobPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete storage blob {BlobPath} for document {DocumentId}",
                doc.BlobPath, doc.Id);
        }

        db.Set<DocumentMetadata>().Remove(doc);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
