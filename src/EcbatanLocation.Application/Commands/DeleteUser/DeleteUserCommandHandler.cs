using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Services;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Commands.DeleteUser;

public class DeleteUserCommandHandler(
    IUserService userService,
    IOwnerRepository ownerRepository,
    IReservationRepository reservationRepository) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var owner = await ownerRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (owner is not null)
        {
            var hasReservations = await reservationRepository.ExistsByOwnerAsync(owner.Id, cancellationToken);
            if (hasReservations)
                throw new InvalidOperationException(
                    "Impossible de supprimer cet utilisateur : il possède des réservations. Supprimez-les d'abord.");

            await ownerRepository.DeleteAsync(owner, cancellationToken);
        }

        var user = await userService.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new InvalidOperationException("Utilisateur introuvable.");

        if (user.Roles.Contains("Admin"))
        {
            var adminCount = await userService.CountUsersInRoleAsync("Admin", cancellationToken);
            if (adminCount <= 1)
                throw new InvalidOperationException(
                    "Impossible de supprimer le dernier administrateur.");
        }

        await userService.DeleteAsync(request.UserId, cancellationToken);
    }
}
