using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Queries.GetStudios;

public class GetStudiosQueryHandler(
    IStudioRepository studioRepository,
    IReservationRepository reservationRepository) : IRequestHandler<GetStudiosQuery, IReadOnlyList<StudioDto>>
{
    public async Task<IReadOnlyList<StudioDto>> Handle(GetStudiosQuery request, CancellationToken cancellationToken)
    {
        var studios = await studioRepository.GetAllAsync(cancellationToken);

        var results = new List<StudioDto>(studios.Count);
        foreach (var s in studios.OrderBy(s => s.DisplayOrder))
        {
            var hasReservations = await reservationRepository.ExistsByStudioAsync(s.Id, cancellationToken);
            results.Add(new StudioDto(s.Id, s.Name, s.Capacity, s.HasKitchen, s.RentableAlone, s.Unavailable, s.DisplayOrder, hasReservations));
        }

        return results;
    }
}
