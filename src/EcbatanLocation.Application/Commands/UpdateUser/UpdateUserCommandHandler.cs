using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Services;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Commands.UpdateUser;

public class UpdateUserCommandHandler(
    IUserService userService,
    IOwnerRepository ownerRepository) : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        await userService.UpdateAsync(
            request.UserId, request.DisplayName, request.Email, request.Roles, cancellationToken);

        var existingOwner = await ownerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var shouldBeOwner = request.Roles.Contains("Owner");

        if (shouldBeOwner && existingOwner is null)
        {
            var owner = Owner.Create(request.DisplayName, request.UserId);
            await ownerRepository.AddAsync(owner, cancellationToken);
        }
        else if (shouldBeOwner && existingOwner is not null)
        {
            existingOwner.Update(request.DisplayName);
            await ownerRepository.UpdateAsync(existingOwner, cancellationToken);
        }
        else if (!shouldBeOwner && existingOwner is not null)
        {
            await ownerRepository.DeleteAsync(existingOwner, cancellationToken);
        }
    }
}
