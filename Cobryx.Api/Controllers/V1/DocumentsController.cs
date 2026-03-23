using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Documents.Commands.DeleteDocument;
using Cobryx.Application.Documents.Commands.UploadDocument;
using Cobryx.Application.Documents.Queries.GetDocumentDownload;
using Cobryx.Application.Documents.Queries.GetDocumentStatus;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Manages document uploads with async virus scanning.
/// Files are uploaded, queued for background scanning, and only downloadable after passing virus check.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/documents")]
[Tags("Documents")]
public class DocumentsController : CobryxBaseController
{
    public DocumentsController(ISender sender) : base(sender) { }

    /// <summary>
    /// Upload a document. Returns 202 with document ID and PendingScan status.
    /// The file will be scanned asynchronously in the background.
    /// </summary>
    /// <param name="entityId">The related entity ID (e.g. customer, invoice).</param>
    /// <param name="entityType">The type of related entity (e.g. "Customer", "Invoice").</param>
    /// <param name="file">The file to upload (max 10 MB).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiSuccessResponse<UploadDocumentResult>), 202)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] Guid entityId,
        [FromForm] string entityType,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponseFactory.Error(errorCode: "VALIDATION.FILE.REQUIRED", numericCode: 4000, traceId: HttpContext.TraceIdentifier));

        using var stream = file.OpenReadStream();

        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var tenantId = Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

        var command = new UploadDocumentCommand(
            tenantId,
            entityId,
            entityType,
            stream,
            file.FileName,
            file.ContentType,
            userId);

        var result = await Sender.Send(command, ct);
        return HandleResult(result, DocumentOutcomes.UploadAccepted, 202);
    }

    /// <summary>
    /// Poll the scan status of an uploaded document.
    /// </summary>
    /// <param name="id">Document ID.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(ApiSuccessResponse<DocumentStatusDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetDocumentStatusQuery(id), ct);
        return HandleResult(result, DocumentOutcomes.StatusRetrieved);
    }

    /// <summary>
    /// Get a pre-signed download URL. Only available when scan status is Clean.
    /// </summary>
    /// <param name="id">Document ID.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{id}/download")]
    [ProducesResponseType(typeof(ApiSuccessResponse<DocumentDownloadDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetDocumentDownloadQuery(id), ct);
        return HandleResult(result, DocumentOutcomes.DownloadReady);
    }

    /// <summary>
    /// Delete a document and its storage blob.
    /// </summary>
    /// <param name="id">Document ID.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeleteDocumentCommand(id), ct);
        return HandleDeleteResult(result);
    }
}

