using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Services;

namespace EcbatanLocation.Application.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    IUserService userService) : IRequestHandler<ResetPasswordCommand, string>
{
    public async Task<string> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        return await userService.ResetPasswordAsync(request.UserId, cancellationToken);
    }
}
