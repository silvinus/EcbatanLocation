using MediatR;
using PlanningLocation.Application.DTOs;
using PlanningLocation.Domain.Repositories;

namespace PlanningLocation.Application.Queries.GetOwnerByUserId;

public class GetOwnerByUserIdQueryHandler(IOwnerRepository ownerRepository)
    : IRequestHandler<GetOwnerByUserIdQuery, OwnerDto?>
{
    public async Task<OwnerDto?> Handle(GetOwnerByUserIdQuery request, CancellationToken cancellationToken)
    {
        var owner = await ownerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return owner is null ? null : new OwnerDto(owner.Id, owner.Name);
    }
}
