namespace Cobryx.Domain.Shared
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; }
        public DateTime CreatedAt { get; internal set; }
        public Guid? CreatedBy { get; private set; }
        public DateTime? UpdatedAt { get; internal set; }
        public Guid? UpdatedBy { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; internal set; }
        public long Version { get; private set; }
        public string? Tags { get; private set; }
        public string? MetadataJson { get; protected set; }
        public string? InternalNotes { get; private set; }

        private readonly List<IDomainEvent> _domainEvents = [];
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected BaseEntity()
        {
            Id = Guid.NewGuid();
        }

        public void SetCreatedBy(Guid userId)
        {
            if (CreatedBy.HasValue)
            {
                return;
            }

            CreatedBy = userId;
        }

        public void SetUpdatedBy(Guid userId) => SetUpdatedBy(userId, DateTime.UtcNow);

        public void SetUpdatedBy(Guid userId, DateTime now)
        {
            UpdatedBy = userId;
            UpdatedAt = now;
        }

        public void Delete() => Delete(DateTime.UtcNow);

        public void Delete(DateTime now)
        {
            IsDeleted = true;
            DeletedAt = now;
            UpdateTimestamp(now);
        }

        public void UpdateTimestamp() => UpdateTimestamp(DateTime.UtcNow);

        public void UpdateTimestamp(DateTime now) => UpdatedAt = now;

        public void IncrementVersion() => Version++;

        public void AddMetadata(string key, string value)
        {
            MetadataJson = string.IsNullOrEmpty(MetadataJson)
                ? $"{key}={value}"
                : $"{MetadataJson};{key}={value}";
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

        protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
