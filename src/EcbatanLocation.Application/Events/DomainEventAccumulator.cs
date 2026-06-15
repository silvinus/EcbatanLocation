using EcbatanLocation.Domain.Events;

namespace EcbatanLocation.Application.Events;

/// <inheritdoc />
internal sealed class DomainEventAccumulator : IDomainEventAccumulator
{
    private readonly List<IDomainEvent> _events = [];

    public void AddRange(IEnumerable<IDomainEvent> domainEvents) => _events.AddRange(domainEvents);

    public IReadOnlyList<IDomainEvent> Collect()
    {
        if (_events.Count == 0)
            return [];

        var collected = _events.ToArray();
        _events.Clear();
        return collected;
    }
}
