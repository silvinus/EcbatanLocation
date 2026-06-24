using EcbatanLocation.Application;
using EcbatanLocation.Application.Events;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace EcbatanLocation.Application.Tests.Messaging;

public class MediatorDomainEventDispatchTests
{
    [Fact]
    public async Task Send_DispatchesBestEffortEvents_AfterCommit()
    {
        var trace = new List<string>();
        var mediator = BuildMediator(trace);

        await mediator.Send(new RecordingRequest());

        Assert.Equal(["handler", "commit", "best-effort-consumer"], trace);
    }

    [Fact]
    public async Task Send_DoesNotDispatchEvents_WhenHandlerThrows()
    {
        var trace = new List<string>();
        var mediator = BuildMediator(trace);

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new ThrowingRequest()));

        Assert.DoesNotContain("best-effort-consumer", trace);
        Assert.DoesNotContain("critical-consumer", trace);
    }

    [Fact]
    public async Task Send_SwallowsFailingBestEffortConsumer()
    {
        var trace = new List<string>();
        var mediator = BuildMediator(trace, failBestEffort: true);

        await mediator.Send(new RecordingRequest());

        Assert.Contains("handler", trace);
    }

    [Fact]
    public async Task Send_CriticalConsumer_RunsBeforeCommit()
    {
        var trace = new List<string>();
        var mediator = BuildMediator(trace, withCritical: true);

        await mediator.Send(new RecordingRequest());

        Assert.Equal(["handler", "critical-consumer", "commit", "best-effort-consumer"], trace);
    }

    [Fact]
    public async Task Send_CriticalConsumerFailure_RollsBackTransaction()
    {
        var trace = new List<string>();
        var mediator = BuildMediator(trace, withCritical: true, failCritical: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new RecordingRequest()));

        Assert.Contains("handler", trace);
        Assert.Contains("critical-consumer", trace);
        Assert.Contains("rollback", trace);
        Assert.DoesNotContain("commit", trace);
        Assert.DoesNotContain("best-effort-consumer", trace);
    }

    private static IMediator BuildMediator(
        List<string> trace,
        bool failBestEffort = false,
        bool withCritical = false,
        bool failCritical = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(trace);
        services.AddSingleton(new ConsumerOptions(failBestEffort, failCritical));
        services.AddScoped<IUnitOfWork>(_ => new TracingUnitOfWork(trace));
        services.AddTransient<IRequestHandler<RecordingRequest, Unit>, RecordingRequestHandler>();
        services.AddTransient<IRequestHandler<ThrowingRequest, Unit>, ThrowingRequestHandler>();
        services.AddTransient<INotificationHandler<DomainEventNotification<TestDomainEvent>>, BestEffortConsumer>();

        if (withCritical)
            services.AddTransient<ICriticalNotificationConsumer<DomainEventNotification<TestDomainEvent>>, CriticalConsumer>();

        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    private sealed record RecordingRequest : IRequest;

    private sealed record ThrowingRequest : IRequest;

    private sealed record TestDomainEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    private sealed record ConsumerOptions(bool FailBestEffort, bool FailCritical);

    private sealed class TracingUnitOfWork(List<string> trace) : IUnitOfWork
    {
        public Task BeginTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task CommitAsync(CancellationToken ct = default)
        {
            trace.Add("commit");
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken ct = default)
        {
            trace.Add("rollback");
            return Task.CompletedTask;
        }
    }

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

    private sealed class CriticalConsumer(List<string> trace, ConsumerOptions options)
        : ICriticalNotificationConsumer<DomainEventNotification<TestDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TestDomainEvent> notification, CancellationToken cancellationToken)
        {
            trace.Add("critical-consumer");

            if (options.FailCritical)
                throw new InvalidOperationException("critical consumer failed");

            return Task.CompletedTask;
        }
    }

    private sealed class BestEffortConsumer(List<string> trace, ConsumerOptions options)
        : INotificationHandler<DomainEventNotification<TestDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TestDomainEvent> notification, CancellationToken cancellationToken)
        {
            if (options.FailBestEffort)
                throw new InvalidOperationException("best-effort consumer failed");

            trace.Add("best-effort-consumer");
            return Task.CompletedTask;
        }
    }
}
