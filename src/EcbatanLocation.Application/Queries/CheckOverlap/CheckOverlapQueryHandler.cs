using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Enums;
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

            var confirmed = overlapping.Where(r => r.Status == ReservationStatus.Confirmed).ToList();
            var availableBedsOverConfirmed = studio.NumberOfBeds - confirmed.Sum(r => r.BedCount);
            var availableCapacityOverConfirmed = studio.Capacity - confirmed.Sum(r => r.TotalAdultCount);

            return new OverlapCheckResult(
                IsPerBed: true,
                HasConflict: availableBeds <= 0,
                AvailableBeds: Math.Max(0, availableBeds),
                AvailableCapacity: Math.Max(0, availableCapacity),
                HasConfirmedConflict: availableBedsOverConfirmed <= 0,
                AvailableBedsOverConfirmed: Math.Max(0, availableBedsOverConfirmed),
                AvailableCapacityOverConfirmed: Math.Max(0, availableCapacityOverConfirmed));
        }

        var overlappingWhole = await reservationRepository.GetOverlappingByStudioAsync(
            request.StudioId, dates, request.ExcludeReservationId, cancellationToken);

        return new OverlapCheckResult(
            IsPerBed: false,
            HasConflict: overlappingWhole.Count > 0,
            AvailableBeds: 0,
            AvailableCapacity: 0,
            HasConfirmedConflict: overlappingWhole.Any(r => r.Status == ReservationStatus.Confirmed),
            AvailableBedsOverConfirmed: 0,
            AvailableCapacityOverConfirmed: 0);
    }
}
