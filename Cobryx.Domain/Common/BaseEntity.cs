using System;
using System.Collections.Generic;

namespace Cobryx.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public uint RowVersion { get; private set; }

    // Production Metadata Enrichment
    public string? Tags { get; private set; } // Comma-separated or structured
    public string? MetadataJson { get; private set; } // Flex-storage for production integrations
    public string? InternalNotes { get; private set; } // System-level documentation

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        // CreatedAt is now initialized directly on the property
    }

    public void SetCreatedBy(Guid userId)
    {
        if (CreatedBy.HasValue) return;
        CreatedBy = userId;
        // CreatedAt is already set by property initializer, no need to set here unless it's meant to be updated on first set.
        // Based on the snippet, it seems the intent was to set it here, but the property initializer makes it redundant.
        // Keeping the original logic for CreatedAt to be set only once.
    }

    public void SetUpdatedBy(Guid userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        UpdateTimestamp();
    }

    public void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;

    public void AddMetadata(string key, string value)
    {
        // Simple logic for metadata enrichment (could be JSON)
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
