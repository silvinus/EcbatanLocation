using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Queries.CheckOverlap;

public class CheckOverlapQueryHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository) : IRequestHandler<CheckOverlapQuery, OverlapCheckResult>
{
    public async Task<OverlapCheckResult> Handle(CheckOverlapQuery request, CancellationToken cancellationToken)
    {
        var dates = new DateRange(request.StartDate, request.EndDate);
        var studio = await studioRepository.GetByIdAsync(request.StudioId, cancellationToken);

        if (studio is not null && studio.IsPerBed)
        {
            var overlapping = await reservationRepository.GetOverlappingByStudioAsync(
                request.StudioId, dates, request.ExcludeReservationId, cancellationToken);

            var availableBeds = studio.NumberOfBeds - overlapping.Sum(r => r.BedCount);
            var availableCapacity = studio.Capacity - overlapping.Sum(r => r.TotalAdultCount);

            return new OverlapCheckResult(
                IsPerBed: true,
                HasConflict: availableBeds <= 0,
                AvailableBeds: Math.Max(0, availableBeds),
                AvailableCapacity: Math.Max(0, availableCapacity));
        }

        var hasOverlap = await reservationRepository.ExistsOverlapAsync(
            request.StudioId, dates, request.ExcludeReservationId, cancellationToken);

        return new OverlapCheckResult(
            IsPerBed: false,
            HasConflict: hasOverlap,
            AvailableBeds: 0,
            AvailableCapacity: 0);
    }
}
