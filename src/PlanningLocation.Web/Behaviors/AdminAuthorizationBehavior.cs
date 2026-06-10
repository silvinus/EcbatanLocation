using PlanningLocation.Application.Messaging;
using Microsoft.AspNetCore.Components.Authorization;
using PlanningLocation.Application.Behaviors;

namespace PlanningLocation.Web.Behaviors;

public class AdminAuthorizationBehavior<TRequest, TResponse>(
    AuthenticationStateProvider authStateProvider) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequireAdmin
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();

        if (state.User.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Vous devez être connecté pour effectuer cette action.");

        if (!state.User.IsInRole("Admin"))
            throw new UnauthorizedAccessException("Cette action est réservée aux administrateurs.");

        return await next(cancellationToken);
    }
}
