using MediatR;
using PlanningLocation.Application.DTOs;
using PlanningLocation.Domain.Repositories;

namespace PlanningLocation.Application.Queries.GetOwners;

public class GetOwnersQueryHandler(
    IOwnerRepository ownerRepository) : IRequestHandler<GetOwnersQuery, IReadOnlyList<OwnerDto>>
{
    public async Task<IReadOnlyList<OwnerDto>> Handle(GetOwnersQuery request, CancellationToken cancellationToken)
    {
        var owners = await ownerRepository.GetAllAsync(cancellationToken);

        return owners
            .Select(o => new OwnerDto(o.Id, o.Name))
            .ToList();
    }
}
