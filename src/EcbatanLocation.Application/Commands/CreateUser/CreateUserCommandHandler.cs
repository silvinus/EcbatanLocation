using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Services;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Commands.CreateUser;

public class CreateUserCommandHandler(
    IUserService userService,
    IOwnerRepository ownerRepository) : IRequestHandler<CreateUserCommand, CreatedUserResult>
{
    public async Task<CreatedUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var result = await userService.CreateAsync(
            request.DisplayName, request.Email, request.Roles, cancellationToken);

        if (request.Roles.Contains("Owner"))
        {
            var owner = Owner.Create(request.DisplayName, result.UserId);
            await ownerRepository.AddAsync(owner, cancellationToken);
        }

        return result;
    }
}
