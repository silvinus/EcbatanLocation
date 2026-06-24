using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EcbatanLocation.Application.Events;
using EcbatanLocation.Domain.Events;

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
///
/// Dispatch uses cached strongly-typed wrappers (one per request / notification / domain-event
/// type, built once via reflection then reused) so the hot path is plain interface calls — no
/// <c>MethodInfo.Invoke</c>, no per-call <c>MakeGenericType</c>, and exceptions propagate naturally.
/// </summary>
public sealed class Mediator(IServiceProvider provider, IServiceScopeFactory scopeFactory) : IMediator
{
    private readonly IServiceProvider _provider = provider;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    // Wrappers are stateless and keyed by runtime type, so a process-wide cache lets every scoped
    // Mediator instance reuse the same closed-generic adapter rather than rebuilding it per request.
    private static readonly ConcurrentDictionary<Type, object> RequestWrappers = new();
    private static readonly ConcurrentDictionary<Type, DomainEventDispatcher> EventDispatchers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestHandlerWrapper<TResponse>)RequestWrappers.GetOrAdd(
            request.GetType(),
            static requestType => Activator.CreateInstance(
                typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        return wrapper.Handle(this, request, cancellationToken);
    }

    public Task Send(IRequest request, CancellationToken cancellationToken = default)
        => Send<Unit>(request, cancellationToken);

    /// <summary>
    /// Terminal pipeline step: runs the handler in a fresh child scope, inside a UoW transaction,
    /// with the two-phase domain-event dispatch wrapped around commit.
    /// </summary>
    private async Task<TResponse> ExecuteHandler<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        using var scope = _scopeFactory.CreateScope();
        var scopedProvider = scope.ServiceProvider;
        var unitOfWork = scopedProvider.GetRequiredService<IUnitOfWork>();

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var handler = scopedProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
            var response = await handler.Handle(request, cancellationToken);

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
            await GetEventDispatcher(domainEvent.GetType()).DispatchCritical(domainEvent, sp, ct);
    }

    private static async Task DispatchBestEffortEventsAsync(IServiceProvider sp, CancellationToken ct)
    {
        var accumulator = sp.GetRequiredService<IDomainEventAccumulator>();
        var events = accumulator.CollectBestEffort();
        if (events.Count == 0) return;

        var logger = sp.GetService<ILogger<Mediator>>();

        foreach (var domainEvent in events)
            await GetEventDispatcher(domainEvent.GetType()).DispatchBestEffort(domainEvent, sp, logger, ct);
    }

    private static DomainEventDispatcher GetEventDispatcher(Type domainEventType)
        => EventDispatchers.GetOrAdd(
            domainEventType,
            static t => (DomainEventDispatcher)Activator.CreateInstance(
                typeof(DomainEventDispatcherImpl<>).MakeGenericType(t))!);

    // ── Request dispatch ────────────────────────────────────────────────────────────────────────

    /// <summary>Type-erased entry point cached per request type; the impl closes over the concrete types.</summary>
    private abstract class RequestHandlerWrapper<TResponse>
    {
        public abstract Task<TResponse> Handle(Mediator mediator, IRequest<TResponse> request, CancellationToken ct);
    }

    private sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Handle(Mediator mediator, IRequest<TResponse> request, CancellationToken ct)
        {
            var typedRequest = (TRequest)request;

            // Behaviors resolved from the caller's scope (preserves circuit services); the handler
            // itself runs in the child scope created by ExecuteHandler.
            var behaviors = mediator._provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

            RequestHandlerDelegate<TResponse> pipeline =
                c => mediator.ExecuteHandler<TRequest, TResponse>(typedRequest, c);

            for (var i = behaviors.Length - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var next = pipeline;
                pipeline = c => behavior.Handle(typedRequest, next, c);
            }

            return pipeline(ct);
        }
    }

    // ── Domain-event dispatch (two-phase) ───────────────────────────────────────────────────────

    private abstract class DomainEventDispatcher
    {
        public abstract Task DispatchCritical(IDomainEvent domainEvent, IServiceProvider sp, CancellationToken ct);
        public abstract Task DispatchBestEffort(IDomainEvent domainEvent, IServiceProvider sp, ILogger? logger, CancellationToken ct);
    }

    private sealed class DomainEventDispatcherImpl<TDomainEvent> : DomainEventDispatcher
        where TDomainEvent : IDomainEvent
    {
        public override async Task DispatchCritical(IDomainEvent domainEvent, IServiceProvider sp, CancellationToken ct)
        {
            var notification = new DomainEventNotification<TDomainEvent>((TDomainEvent)domainEvent);
            foreach (var consumer in sp.GetServices<ICriticalNotificationConsumer<DomainEventNotification<TDomainEvent>>>())
                await consumer.Handle(notification, ct);
        }

        public override async Task DispatchBestEffort(IDomainEvent domainEvent, IServiceProvider sp, ILogger? logger, CancellationToken ct)
        {
            var notification = new DomainEventNotification<TDomainEvent>((TDomainEvent)domainEvent);
            foreach (var handler in sp.GetServices<INotificationHandler<DomainEventNotification<TDomainEvent>>>())
            {
                try
                {
                    await handler.Handle(notification, ct);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Domain event handler {Handler} failed for {DomainEvent}.",
                        handler.GetType().Name, typeof(TDomainEvent).Name);
                }
            }
        }
    }
}
