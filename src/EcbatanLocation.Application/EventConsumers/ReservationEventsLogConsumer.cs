using EcbatanLocation.Application.Events;
using EcbatanLocation.Application.Messaging;
using Microsoft.Extensions.Logging;
using EcbatanLocation.Domain.Events;

namespace EcbatanLocation.Application.EventConsumers;

public sealed class ReservationEventsLogConsumer(ILogger<ReservationEventsLogConsumer> logger)
    : INotificationHandler<DomainEventNotification<ReservationCreated>>,
      INotificationHandler<DomainEventNotification<ReservationAccepted>>,
      INotificationHandler<DomainEventNotification<ReservationConfirmed>>,
      INotificationHandler<DomainEventNotification<ReservationDeleted>>
{
    public Task Handle(DomainEventNotification<ReservationCreated> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        logger.LogInformation(
            "Reservation {ReservationId} created on studio {StudioId} by owner {OwnerId}.",
            e.ReservationId, e.StudioId, e.OwnerId);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<ReservationAccepted> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        logger.LogInformation("Reservation {ReservationId} accepted by {AcceptedBy}.", e.ReservationId, e.AcceptedBy);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<ReservationConfirmed> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        logger.LogInformation("Reservation {ReservationId} confirmed by {ConfirmedBy}.", e.ReservationId, e.ConfirmedBy);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<ReservationDeleted> notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reservation {ReservationId} deleted.", notification.DomainEvent.ReservationId);
        return Task.CompletedTask;
    }
}
