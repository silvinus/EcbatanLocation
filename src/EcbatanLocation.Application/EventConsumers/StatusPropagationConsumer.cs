using EcbatanLocation.Application.Events;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Events;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.Services;

namespace EcbatanLocation.Application.EventConsumers;

public sealed class StatusPropagationConsumer(
    IReservationRepository reservationRepository,
    ReservationDomainService domainService)
    : ICriticalNotificationConsumer<DomainEventNotification<ReservationAccepted>>,
      ICriticalNotificationConsumer<DomainEventNotification<ReservationConfirmed>>
{
    public async Task Handle(
        DomainEventNotification<ReservationAccepted> notification,
        CancellationToken cancellationToken)
        => await PropagateAsync(notification.DomainEvent.ReservationId, cancellationToken);

    public async Task Handle(
        DomainEventNotification<ReservationConfirmed> notification,
        CancellationToken cancellationToken)
        => await PropagateAsync(notification.DomainEvent.ReservationId, cancellationToken);

    private async Task PropagateAsync(Guid parentId, CancellationToken ct)
    {
        var dependents = await reservationRepository.GetDependentsByParentIdAsync(parentId, ct);
        if (dependents.Count == 0) return;

        var parent = await reservationRepository.GetByIdAsync(parentId, ct)
            ?? throw new InvalidOperationException($"Reservation '{parentId}' not found.");

        domainService.PropagateStatusToDependents(parent, dependents);
        await reservationRepository.UpdateRangeAsync(dependents, ct);
    }
}
