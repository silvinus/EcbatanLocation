using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Queries.GetCompatibleParents;

public class GetCompatibleParentsQueryHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository) : IRequestHandler<GetCompatibleParentsQuery, IReadOnlyList<ParentReservationOptionDto>>
{
    public async Task<IReadOnlyList<ParentReservationOptionDto>> Handle(
        GetCompatibleParentsQuery request, CancellationToken cancellationToken)
    {
        var dates = new DateRange(request.StartDate, request.EndDate);
        var parents = await reservationRepository.GetCompatibleParentsAsync(
            request.OwnerId, dates, request.ExcludeReservationId, cancellationToken);

        var studios = await studioRepository.GetAllAsync(cancellationToken);
        var studioLookup = studios.ToDictionary(s => s.Id, s => s.Name);

        return parents
            .Select(r => new ParentReservationOptionDto(
                r.Id,
                studioLookup.GetValueOrDefault(r.StudioId, "Unknown"),
                r.TenantName,
                r.Dates.StartDate,
                r.Dates.EndDate,
                r.Status))
            .ToList();
    }
}
