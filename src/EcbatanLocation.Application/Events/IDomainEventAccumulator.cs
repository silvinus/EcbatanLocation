using EcbatanLocation.Domain.Events;

namespace EcbatanLocation.Application.Events;

/// <summary>
/// Per-operation (scoped) buffer for domain events raised by aggregates during a unit of work.
/// The persistence interceptor stashes events here when changes are saved; the mediator drains
/// and dispatches them <b>after</b> the handler completes (post-commit). Scoped lifetime ties one
/// accumulator to one Command/Query execution, so events never leak between concurrent operations.
/// </summary>
public interface IDomainEventAccumulator
{
    /// <summary>Buffers events collected while saving changes.</summary>
    void AddRange(IEnumerable<IDomainEvent> domainEvents);

    /// <summary>Returns the buffered events and empties the buffer.</summary>
    IReadOnlyList<IDomainEvent> Collect();
}
