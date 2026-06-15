using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using EcbatanLocation.Application.Events;
using EcbatanLocation.Domain.Common;

namespace EcbatanLocation.Infrastructure.Persistence;

/// <summary>
/// Collects domain events from tracked aggregates when changes are saved and hands them to the
/// scoped <see cref="IDomainEventAccumulator"/>. Dispatch happens later — after the handler's
/// transaction has committed (see <c>Mediator</c>). Events therefore fire only on success and are
/// never executed inside the aggregate's write transaction, keeping side effects (notifications,
/// logging) decoupled from persistence.
/// </summary>
public sealed class DomainEventCollectorInterceptor(IDomainEventAccumulator accumulator) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            CollectDomainEvents(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CollectDomainEvents(DbContext context)
    {
        var aggregates = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (aggregates.Count == 0)
            return;

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        accumulator.AddRange(domainEvents);
    }
}
