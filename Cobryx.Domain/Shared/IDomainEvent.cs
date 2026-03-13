namespace Cobryx.Domain.Shared;

public interface IDomainEvent
{
    public DateTime OccurredOn { get; }
}
