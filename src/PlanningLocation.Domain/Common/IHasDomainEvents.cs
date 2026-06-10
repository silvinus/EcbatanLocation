using PlanningLocation.Domain.Events;

namespace PlanningLocation.Domain.Common;

/// <summary>Implemented by aggregates that record domain events to be dispatched after persistence.</summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
