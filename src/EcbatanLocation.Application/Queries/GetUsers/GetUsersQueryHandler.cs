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
        var ownerByUserId = owners.ToDictionary(o => o.UserId);

        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var isOwner = ownerByUserId.ContainsKey(user.UserId);
            var hasReservations = false;

            if (isOwner)
            {
                var owner = ownerByUserId[user.UserId];
                hasReservations = await reservationRepository.ExistsByOwnerAsync(owner.Id, cancellationToken);
            }

            result.Add(new UserDto(
                user.UserId,
                user.DisplayName,
                user.Email,
                user.Roles,
                isOwner,
                hasReservations));
        }

        return result;
    }
}
