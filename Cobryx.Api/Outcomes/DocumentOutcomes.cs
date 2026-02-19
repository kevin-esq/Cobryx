using Cobryx.Domain.Common;

namespace Cobryx.Api.Outcomes;

public static class DocumentOutcomes
{
    private const string Prefix = "DOCUMENTS";

    public static readonly Outcome UploadAccepted = new($"{Prefix}.UPLOAD_ACCEPTED", OutcomeCategory.Success, "Document uploaded and queued for scanning.");
    public static readonly Outcome StatusRetrieved = new($"{Prefix}.STATUS_RETRIEVED", OutcomeCategory.Success, "Document status retrieved.");
    public static readonly Outcome DownloadReady = new($"{Prefix}.DOWNLOAD_READY", OutcomeCategory.Success, "Document download URL generated.");
    public static readonly Outcome DeleteSuccess = new($"{Prefix}.DELETE_SUCCESS", OutcomeCategory.Success, "Document deleted.");
}
