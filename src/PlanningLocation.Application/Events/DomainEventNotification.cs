using MediatR;
using PlanningLocation.Domain.Events;

namespace PlanningLocation.Application.Events;

/// <summary>
/// MediatR wrapper around a pure domain event, letting the Domain layer stay free of MediatR.
/// Handlers implement <c>INotificationHandler&lt;DomainEventNotification&lt;TDomainEvent&gt;&gt;</c>.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
