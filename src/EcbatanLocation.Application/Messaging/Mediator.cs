using Microsoft.Extensions.DependencyInjection;

namespace EcbatanLocation.Application.Messaging;

/// <summary>
/// Minimal in-process mediator. Resolves the request handler and its pipeline behaviors
/// from the current scope and composes them (first-registered behavior is outermost).
/// </summary>
public sealed class Mediator(IServiceProvider provider) : IMediator
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = provider.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod("Handle")!;

        RequestHandlerDelegate<TResponse> pipeline =
            ct => (Task<TResponse>)handleMethod.Invoke(handler, [request, ct])!;

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviorHandle = behaviorType.GetMethod("Handle")!;
        var behaviors = provider.GetServices(behaviorType).ToArray();

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i]!;
            var next = pipeline;
            pipeline = ct => (Task<TResponse>)behaviorHandle.Invoke(behavior, [request, next, ct])!;
        }

        return pipeline(cancellationToken);
    }

    public Task Send(IRequest request, CancellationToken cancellationToken = default)
        => Send<Unit>(request, cancellationToken);

    public async Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
        var handleMethod = handlerType.GetMethod("Handle")!;

        foreach (var handler in provider.GetServices(handlerType))
            await (Task)handleMethod.Invoke(handler!, [notification, cancellationToken])!;
    }
}
