using EcbatanLocation.Domain.Events;

namespace EcbatanLocation.Application.Events;

/// <inheritdoc />
internal sealed class DomainEventAccumulator : IDomainEventAccumulator
{
    private readonly List<IDomainEvent> _events = [];
    private readonly List<IDomainEvent> _bestEffortEvents = [];

    public void AddRange(IEnumerable<IDomainEvent> domainEvents) => _events.AddRange(domainEvents);

    public IReadOnlyList<IDomainEvent> Collect()
    {
        if (_events.Count == 0)
            return [];

        var collected = _events.ToArray();
        _events.Clear();
        return collected;
    }

    public void StoreForBestEffort(IReadOnlyList<IDomainEvent> events)
        => _bestEffortEvents.AddRange(events);

    public IReadOnlyList<IDomainEvent> CollectBestEffort()
    {
        if (_bestEffortEvents.Count == 0)
            return [];

        var collected = _bestEffortEvents.ToArray();
        _bestEffortEvents.Clear();
        return collected;
    }
}
