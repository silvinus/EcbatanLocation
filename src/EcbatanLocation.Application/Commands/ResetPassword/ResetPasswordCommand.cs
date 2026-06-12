using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.Messaging;

namespace EcbatanLocation.Application.Commands.ResetPassword;

public record ResetPasswordCommand(string UserId) : IRequest<string>, IRequireAdmin;
