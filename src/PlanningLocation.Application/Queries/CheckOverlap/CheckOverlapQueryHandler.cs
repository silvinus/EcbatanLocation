using MediatR;
using PlanningLocation.Domain.Repositories;
using PlanningLocation.Domain.ValueObjects;

namespace PlanningLocation.Application.Queries.CheckOverlap;

public class CheckOverlapQueryHandler(
    IReservationRepository reservationRepository) : IRequestHandler<CheckOverlapQuery, bool>
{
    public async Task<bool> Handle(CheckOverlapQuery request, CancellationToken cancellationToken)
    {
        var dates = new DateRange(request.StartDate, request.EndDate);
        return await reservationRepository.ExistsOverlapAsync(
            request.StudioId, dates, request.ExcludeReservationId, cancellationToken);
    }
}
