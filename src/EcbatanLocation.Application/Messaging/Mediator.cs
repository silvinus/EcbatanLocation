using Microsoft.Extensions.DependencyInjection;

namespace EcbatanLocation.Application.Messaging;

/// <summary>
/// Minimal in-process mediator. Resolves pipeline behaviors from the caller's scope
/// (to preserve Blazor circuit services like AuthenticationStateProvider), but executes
/// the handler in a fresh child scope so that scoped services (DbContext) are isolated
/// per operation — required for Blazor Server where the circuit scope is long-lived.
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

        return await (Task<TResponse>)handleMethod.Invoke(handler, [request, cancellationToken])!;
    }
}
