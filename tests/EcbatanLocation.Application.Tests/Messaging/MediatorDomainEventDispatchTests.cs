using EcbatanLocation.Application;
using EcbatanLocation.Application.Events;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace EcbatanLocation.Application.Tests.Messaging;

/// <summary>
/// Locks down the post-commit domain-event dispatch contract: events buffered during a handler
/// are dispatched only after the handler returns, never when it throws, and a failing event
/// handler is swallowed so it cannot fail the already-committed operation.
/// </summary>
public class MediatorDomainEventDispatchTests
{
    [Fact]
    public async Task Send_DispatchesBufferedEvents_AfterHandlerCompletes()
    {
        var trace = new List<string>();
        var mediator = BuildMediator(trace);

        await mediator.Send(new RecordingRequest());

        Assert.Equal(["handler", "event-handler"], trace);
    }

    [Fact]
    public async Task Send_DoesNotDispatchEvents_WhenHandlerThrows()
    {
        var trace = new List<string>();
        var mediator = BuildMediator(trace);

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new ThrowingRequest()));

        // The handler buffered an event then threw; dispatch must be skipped.
        Assert.DoesNotContain("event-handler", trace);
    }

    [Fact]
    public async Task Send_SwallowsFailingEventHandler_SoCommittedOperationSucceeds()
    {
        var trace = new List<string>();
        var mediator = BuildMediator(trace, failEventHandler: true);

        // Must not throw even though the post-commit reaction fails (best-effort).
        await mediator.Send(new RecordingRequest());

        Assert.Contains("handler", trace);
    }

    private static IMediator BuildMediator(List<string> trace, bool failEventHandler = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(trace);
        services.AddSingleton(new EventHandlerOptions(failEventHandler));
        services.AddTransient<IRequestHandler<RecordingRequest, Unit>, RecordingRequestHandler>();
        services.AddTransient<IRequestHandler<ThrowingRequest, Unit>, ThrowingRequestHandler>();
        services.AddTransient<INotificationHandler<DomainEventNotification<TestDomainEvent>>, TestEventHandler>();

        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    private sealed record RecordingRequest : IRequest;

    private sealed record ThrowingRequest : IRequest;

    private sealed record TestDomainEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    private sealed record EventHandlerOptions(bool ShouldThrow);

    // Stands in for a repository: records progress and buffers a domain event the way the
    // persistence interceptor would during SaveChanges.
    private sealed class RecordingRequestHandler(List<string> trace, IDomainEventAccumulator accumulator)
        : IRequestHandler<RecordingRequest>
    {
        public Task Handle(RecordingRequest request, CancellationToken cancellationToken)
        {
            trace.Add("handler");
            accumulator.AddRange([new TestDomainEvent()]);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingRequestHandler(List<string> trace, IDomainEventAccumulator accumulator)
        : IRequestHandler<ThrowingRequest>
    {
        public Task Handle(ThrowingRequest request, CancellationToken cancellationToken)
        {
            trace.Add("handler");
            accumulator.AddRange([new TestDomainEvent()]);
            throw new InvalidOperationException("boom");
        }
    }

    private sealed class TestEventHandler(List<string> trace, EventHandlerOptions options)
        : INotificationHandler<DomainEventNotification<TestDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TestDomainEvent> notification, CancellationToken cancellationToken)
        {
            if (options.ShouldThrow)
                throw new InvalidOperationException("notification failed");

            trace.Add("event-handler");
            return Task.CompletedTask;
        }
    }
}
