using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EcbatanLocation.Application.Events;

namespace EcbatanLocation.Application.Messaging;

/// <summary>
/// Minimal in-process mediator. Resolves pipeline behaviors from the caller's scope
/// (to preserve Blazor circuit services like AuthenticationStateProvider), but executes
/// the handler in a fresh child scope so that scoped services (DbContext) are isolated
/// per operation — required for Blazor Server where the circuit scope is long-lived.
///
/// Domain events raised during the handler are buffered by the persistence interceptor in the
/// scoped <see cref="IDomainEventAccumulator"/> and dispatched here <b>after</b> the handler
/// returns — i.e. post-commit. They are best-effort side effects: they fire only when the
/// operation succeeds, run outside its write transaction, and a failing handler is logged but
/// never bubbles up to fail an already-committed operation.
/// </summary>
public sealed class Mediator(IServiceProvider provider, IServiceScopeFactory scopeFactory) : IMediator
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviorHandle = behaviorType.GetMethod("Handle")!;
        var behaviors = provider.GetServices(behaviorType).ToArray();

        RequestHandlerDelegate<TResponse> pipeline = ct => ExecuteHandler<TResponse>(request, ct);

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i]!;
            var next = pipeline;
            pipeline = ct => (Task<TResponse>)behaviorHandle.Invoke(behavior, [request, next, ct])!;
        }

        return await pipeline(cancellationToken);
    }

    public Task Send(IRequest request, CancellationToken cancellationToken = default)
        => Send<Unit>(request, cancellationToken);

    public async Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        using var scope = scopeFactory.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
        var handleMethod = handlerType.GetMethod("Handle")!;

        foreach (var handler in scopedProvider.GetServices(handlerType))
            await (Task)handleMethod.Invoke(handler!, [notification, cancellationToken])!;
    }

    private async Task<TResponse> ExecuteHandler<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = scopedProvider.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod("Handle")!;

        var response = await (Task<TResponse>)handleMethod.Invoke(handler, [request, cancellationToken])!;

        // The handler (and its repository transaction) committed successfully. Drain the events
        // buffered during persistence and dispatch them now — post-commit, in this same scope.
        await DispatchDomainEventsAsync(scopedProvider, cancellationToken);

        return response;
    }

    private static async Task DispatchDomainEventsAsync(IServiceProvider scopedProvider, CancellationToken cancellationToken)
    {
        var domainEvents = scopedProvider.GetRequiredService<IDomainEventAccumulator>().Collect();
        if (domainEvents.Count == 0)
            return;

        var logger = scopedProvider.GetService<ILogger<Mediator>>();

        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handleMethod = handlerType.GetMethod("Handle")!;

            foreach (var handler in scopedProvider.GetServices(handlerType))
            {
                try
                {
                    await (Task)handleMethod.Invoke(handler!, [notification, cancellationToken])!;
                }
                catch (Exception ex)
                {
                    // Best-effort: a failed post-commit reaction must not fail the committed operation.
                    var cause = (ex as TargetInvocationException)?.InnerException ?? ex;
                    logger?.LogWarning(cause, "Domain event handler {Handler} failed for {DomainEvent}.",
                        handler!.GetType().Name, domainEvent.GetType().Name);
                }
            }
        }
    }
}
