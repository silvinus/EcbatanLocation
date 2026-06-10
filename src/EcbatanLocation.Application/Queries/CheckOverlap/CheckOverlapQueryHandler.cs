using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Queries.CheckOverlap;

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
