using PlanningLocation.Application.Messaging;
using Microsoft.AspNetCore.Components.Authorization;
using PlanningLocation.Application.Behaviors;

namespace PlanningLocation.Web.Behaviors;

public class AuthorizationBehavior<TRequest, TResponse>(
    AuthenticationStateProvider authStateProvider) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequireAuthorization
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Vous devez être connecté pour effectuer cette action.");

        return await next(cancellationToken);
    }
}
