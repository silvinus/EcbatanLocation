using MediatR;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Repositories;
using PlanningLocation.Domain.Services;
using PlanningLocation.Domain.ValueObjects;

namespace PlanningLocation.Application.Commands.CreateReservation;

public class CreateReservationCommandHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository,
    ReservationDomainService domainService) : IRequestHandler<CreateReservationCommand, Guid>
{
    public async Task<Guid> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var studio = await studioRepository.GetByIdAsync(request.StudioId, cancellationToken)
                     ?? throw new InvalidOperationException($"Studio '{request.StudioId}' not found.");

        var dates = new DateRange(request.StartDate, request.EndDate);

        var overlapExists = await reservationRepository.ExistsOverlapAsync(
            request.StudioId, dates, null, cancellationToken);
        domainService.ValidateNoOverlap(overlapExists);

        if (!studio.RentableAlone)
        {
            var ownerReservations = await reservationRepository.GetByOwnerAndOverlappingDatesAsync(
                request.OwnerId, dates, cancellationToken);
            domainService.ValidateStudioDependency(studio, request.OwnerId, dates, ownerReservations);
        }

        var reservation = Reservation.Create(
            request.StudioId,
            request.OwnerId,
            dates,
            request.TenantName,
            request.AdultCount,
            request.ChildrenUnder3Count,
            request.ClientType,
            studio.Capacity);

        await reservationRepository.AddAsync(reservation, cancellationToken);

        return reservation.Id;
    }
}
