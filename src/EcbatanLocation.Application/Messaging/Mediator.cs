using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EcbatanLocation.Application.Events;

namespace EcbatanLocation.Application.Messaging;

/// <summary>
/// Minimal in-process mediator with two-phase domain event dispatch. Resolves pipeline behaviors
/// from the caller's scope (to preserve Blazor circuit services like AuthenticationStateProvider),
/// but executes the handler in a fresh child scope so that scoped services (DbContext) are isolated
/// per operation — required for Blazor Server where the circuit scope is long-lived.
///
/// Domain events raised during the handler are dispatched in two phases:
/// 1. <b>Critical</b> handlers (<see cref="ICriticalNotificationConsumer{T}"/>) run inside the
///    same UoW transaction — a failure rolls back the entire operation.
/// 2. <b>Best-effort</b> handlers (<see cref="INotificationHandler{T}"/>) run after commit;
///    failures are logged but never bubble up.
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
        var unitOfWork = scopedProvider.GetRequiredService<IUnitOfWork>();

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var requestType = request.GetType();
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
            var handler = scopedProvider.GetRequiredService(handlerType);
            var handleMethod = handlerType.GetMethod("Handle")!;

            var response = await (Task<TResponse>)handleMethod.Invoke(handler, [request, cancellationToken])!;

            await DispatchCriticalEventsAsync(scopedProvider, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);

            await DispatchBestEffortEventsAsync(scopedProvider, cancellationToken);

            return response;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task DispatchCriticalEventsAsync(IServiceProvider sp, CancellationToken ct)
    {
        var accumulator = sp.GetRequiredService<IDomainEventAccumulator>();
        var events = accumulator.Collect();
        if (events.Count == 0) return;

        accumulator.StoreForBestEffort(events);

        foreach (var domainEvent in events)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

            var handlerType = typeof(ICriticalNotificationConsumer<>).MakeGenericType(notificationType);
            var handleMethod = handlerType.GetMethod("Handle")!;

            foreach (var handler in sp.GetServices(handlerType))
            {
                try
                {
                    await (Task)handleMethod.Invoke(handler!, [notification, ct])!;
                }
                catch (TargetInvocationException ex) when (ex.InnerException is not null)
                {
                    throw ex.InnerException;
                }
            }
        }
    }

    private static async Task DispatchBestEffortEventsAsync(IServiceProvider sp, CancellationToken ct)
    {
        var accumulator = sp.GetRequiredService<IDomainEventAccumulator>();
        var events = accumulator.CollectBestEffort();
        if (events.Count == 0) return;

        var logger = sp.GetService<ILogger<Mediator>>();

        foreach (var domainEvent in events)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handleMethod = handlerType.GetMethod("Handle")!;

            foreach (var handler in sp.GetServices(handlerType))
            {
                try
                {
                    await (Task)handleMethod.Invoke(handler!, [notification, ct])!;
                }
                catch (Exception ex)
                {
                    var cause = (ex as TargetInvocationException)?.InnerException ?? ex;
                    logger?.LogWarning(cause, "Domain event handler {Handler} failed for {DomainEvent}.",
                        handler!.GetType().Name, domainEvent.GetType().Name);
                }
            }
        }
    }
}
