using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Queries.GetStudios;

public class GetStudiosQueryHandler(
    IStudioRepository studioRepository) : IRequestHandler<GetStudiosQuery, IReadOnlyList<StudioDto>>
{
    public async Task<IReadOnlyList<StudioDto>> Handle(GetStudiosQuery request, CancellationToken cancellationToken)
    {
        var studios = await studioRepository.GetAllAsync(cancellationToken);

        return studios
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new StudioDto(s.Id, s.Name, s.Capacity, s.HasKitchen, s.RentableAlone, s.DisplayOrder))
            .ToList();
    }
}
