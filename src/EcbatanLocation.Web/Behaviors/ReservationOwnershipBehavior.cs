using System.Security.Claims;
using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;
using Microsoft.AspNetCore.Components.Authorization;

namespace EcbatanLocation.Web.Behaviors;

/// <summary>
/// Enforces that a command acting on an existing reservation is performed by the reservation's
/// owner or by an administrator. Resolved in the calling (circuit) scope so it can read the user
/// from <see cref="AuthenticationStateProvider"/>; ownership is read through a fresh DI scope so the
/// circuit-scoped <c>DbContext</c> is never touched (preserving the per-handler scope isolation).
/// </summary>
public class ReservationOwnershipBehavior<TRequest, TResponse>(
    AuthenticationStateProvider authStateProvider,
    IHttpContextAccessor httpContextAccessor,
    IServiceScopeFactory scopeFactory) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequireReservationOwnership
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var user = await GetUserAsync();
        if (user.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Vous devez être connecté pour effectuer cette action.");

        // An administrator can act on any reservation.
        if (user.IsInRole("Admin"))
            return await next(cancellationToken);

        using var scope = scopeFactory.CreateScope();
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var ownerRepository = scope.ServiceProvider.GetRequiredService<IOwnerRepository>();

        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);

        // Let the handler raise its own not-found error rather than masking it as an authorization failure.
        if (reservation is null)
            return await next(cancellationToken);

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentOwner = userId is null ? null : await ownerRepository.GetByUserIdAsync(userId, cancellationToken);

        if (currentOwner is null || reservation.OwnerId != currentOwner.Id)
            throw new UnauthorizedAccessException(
                "Vous ne pouvez agir que sur les réservations dont vous êtes propriétaire.");

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
