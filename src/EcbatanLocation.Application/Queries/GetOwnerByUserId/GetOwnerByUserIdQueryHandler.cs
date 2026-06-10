using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Queries.GetOwnerByUserId;

public class GetOwnerByUserIdQueryHandler(IOwnerRepository ownerRepository)
    : IRequestHandler<GetOwnerByUserIdQuery, OwnerDto?>
{
    public async Task<OwnerDto?> Handle(GetOwnerByUserIdQuery request, CancellationToken cancellationToken)
    {
        var owner = await ownerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return owner is null ? null : new OwnerDto(owner.Id, owner.Name);
    }
}
