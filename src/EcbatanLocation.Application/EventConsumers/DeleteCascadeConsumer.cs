using EcbatanLocation.Application.Events;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Events;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.EventConsumers;

public sealed class DeleteCascadeConsumer(IReservationRepository reservationRepository)
    : ICriticalNotificationConsumer<DomainEventNotification<ReservationDeleted>>
{
    public async Task Handle(
        DomainEventNotification<ReservationDeleted> notification,
        CancellationToken cancellationToken)
    {
        var reservationId = notification.DomainEvent.ReservationId;

        var dependents = await reservationRepository.GetDependentsByParentIdAsync(reservationId, cancellationToken);
        if (dependents.Count > 0)
            await reservationRepository.DeleteRangeAsync(dependents.Select(d => d.Id), cancellationToken);

        await reservationRepository.DeleteAsync(reservationId, cancellationToken);
    }
}
