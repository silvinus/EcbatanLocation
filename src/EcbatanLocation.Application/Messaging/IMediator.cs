namespace EcbatanLocation.Application.Messaging;

/// <summary>Represents a void response (no meaningful return value).</summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
}

/// <summary>A request that returns a <typeparamref name="TResponse"/>.</summary>
public interface IRequest<out TResponse>;

/// <summary>A request with no return value.</summary>
public interface IRequest : IRequest<Unit>;

/// <summary>An event published to zero or more handlers.</summary>
public interface INotification;

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>Handler for a void request. Bridges to the <see cref="Unit"/>-returning pipeline.</summary>
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>
    where TRequest : IRequest
{
    new Task Handle(TRequest request, CancellationToken cancellationToken);

    async Task<Unit> IRequestHandler<TRequest, Unit>.Handle(TRequest request, CancellationToken cancellationToken)
    {
        await Handle(request, cancellationToken);
        return Unit.Value;
    }
}

public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

/// <summary>Continuation that invokes the next behavior in the pipeline, or the handler.</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

/// <summary>Cross-cutting behavior wrapping request handling (validation, authorization, ...).</summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>Sends requests to their handler through the behavior pipeline.</summary>
public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task Send(IRequest request, CancellationToken cancellationToken = default);
}
