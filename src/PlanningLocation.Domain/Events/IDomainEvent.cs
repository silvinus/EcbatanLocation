namespace PlanningLocation.Domain.Events;

/// <summary>
/// Marker for domain events. Kept free of any framework dependency so the
/// Domain layer stays dependency-free; dispatching is wired up in outer layers.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
