using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.Messaging;

namespace EcbatanLocation.Application.Commands.DeleteUser;

public record DeleteUserCommand(string UserId) : IRequest, IRequireAdmin;
