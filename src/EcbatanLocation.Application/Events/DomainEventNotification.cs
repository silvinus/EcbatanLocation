using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Events;

namespace EcbatanLocation.Application.Events;

/// <summary>
/// Messaging wrapper around a pure domain event, letting the Domain layer stay free of the
/// messaging infrastructure. Handlers implement
/// <c>INotificationHandler&lt;DomainEventNotification&lt;TDomainEvent&gt;&gt;</c>.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
