using System;

namespace Cobryx.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
