using System.Security.Claims;
using EcbatanLocation.Application.Messaging;
using Microsoft.AspNetCore.Components.Authorization;
using EcbatanLocation.Application.Behaviors;

namespace EcbatanLocation.Web.Behaviors;

public class AdminAuthorizationBehavior<TRequest, TResponse>(
    AuthenticationStateProvider authStateProvider,
    IHttpContextAccessor httpContextAccessor) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequireAdmin
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var user = await GetUserAsync();

        if (user.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Vous devez être connecté pour effectuer cette action.");

        if (!user.IsInRole("Admin"))
            throw new UnauthorizedAccessException("Cette action est réservée aux administrateurs.");

        return await next(cancellationToken);
    }

    private async Task<ClaimsPrincipal> GetUserAsync()
    {
        var httpUser = httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
            return httpUser;

        var state = await authStateProvider.GetAuthenticationStateAsync();
        return state.User;
    }
}
