namespace Cobryx.Domain.Shared;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public long Version { get; private set; }

    public string? Tags { get; private set; }
    public string? MetadataJson { get; protected set; }
    public string? InternalNotes { get; private set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    public void SetCreatedBy(Guid userId)
    {
        if (CreatedBy.HasValue) return;
        CreatedBy = userId;
    }

    public void SetUpdatedBy(Guid userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;

    public void IncrementVersion() => Version++;

    public void AddMetadata(string key, string value)
    {
        MetadataJson = string.IsNullOrEmpty(MetadataJson) ? $"{key}={value}" : $"{MetadataJson};{key}={value}";
        UpdateTimestamp();
    }

    public void SetTags(string tags)
    {
        Tags = tags;
        UpdateTimestamp();
    }

    public void SetInternalNotes(string notes)
    {
        InternalNotes = notes;
        UpdateTimestamp();
    }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
