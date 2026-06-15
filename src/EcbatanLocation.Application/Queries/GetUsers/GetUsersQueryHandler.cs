using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Services;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Queries.GetUsers;

public class GetUsersQueryHandler(
    IUserService userService,
    IOwnerRepository ownerRepository,
    IReservationRepository reservationRepository) : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userService.GetAllUsersAsync(cancellationToken);
        var owners = await ownerRepository.GetAllAsync(cancellationToken);
        var ownerIdsWithReservations = await reservationRepository.GetOwnerIdsWithReservationsAsync(cancellationToken);
        var ownerByUserId = owners.ToDictionary(o => o.UserId);

        return users.Select(user =>
        {
            var isOwner = ownerByUserId.TryGetValue(user.UserId, out var owner);
            var hasReservations = isOwner && ownerIdsWithReservations.Contains(owner!.Id);
            return new UserDto(user.UserId, user.DisplayName, user.Email, user.Roles, isOwner, hasReservations);
        }).ToList();
    }
}
