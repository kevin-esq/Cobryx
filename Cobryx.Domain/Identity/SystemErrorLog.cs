using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Identity;

public class SystemErrorLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Message { get; private set; }
    public string? StackTrace { get; private set; }
    public string? Source { get; private set; }
    public string? RequestPath { get; private set; }
    public string? RequestMethod { get; private set; }
    public string? IpAddress { get; private set; }
    public bool IsResolved { get; private set; }
    public string? ResolutionNotes { get; private set; }

    private SystemErrorLog()
    {
        Message = null!;
    }

    public SystemErrorLog(
        Guid tenantId,
        Guid? userId,
        string message,
        string? stackTrace,
        string? source,
        string? requestPath,
        string? requestMethod,
        string? ipAddress)
    {
        TenantId = tenantId;
        UserId = userId;
        Message = message;
        StackTrace = stackTrace;
        Source = source;
        RequestPath = requestPath;
        RequestMethod = requestMethod;
        IpAddress = ipAddress;
        IsResolved = false;
    }

    public void Resolve(string notes)
    {
        IsResolved = true;
        ResolutionNotes = notes;
        UpdateTimestamp();
    }
}
