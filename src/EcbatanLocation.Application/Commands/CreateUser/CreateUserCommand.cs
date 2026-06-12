using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Services;

namespace EcbatanLocation.Application.Commands.CreateUser;

public record CreateUserCommand(
    string DisplayName,
    string Email,
    IReadOnlyList<string> Roles) : IRequest<CreatedUserResult>, IRequireAdmin;
