using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.Messaging;

namespace EcbatanLocation.Application.Commands.UpdateUser;

public record UpdateUserCommand(
    string UserId,
    string DisplayName,
    string Email,
    IReadOnlyList<string> Roles) : IRequest, IRequireAdmin;
