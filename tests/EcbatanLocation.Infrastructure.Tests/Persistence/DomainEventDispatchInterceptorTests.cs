using EcbatanLocation.Application.Messaging;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EcbatanLocation.Application;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.ValueObjects;
using EcbatanLocation.Infrastructure.Persistence;

namespace EcbatanLocation.Infrastructure.Tests.Persistence;

public class DomainEventDispatchInterceptorTests
{
    [Fact]
    public async Task SavingChanges_PublishesDomainEvents_ThroughMediator()
    {
        var logs = new List<string>();
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddLogging(b => b.AddProvider(new CapturingLoggerProvider(logs)));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<EcbatanLocationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new DomainEventDispatchInterceptor(mediator))
            .Options;

        await using var context = new EcbatanLocationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var studio = Studio.Create("Villa", 6, true, true, 1);
        context.Studios.Add(studio);
        await context.SaveChangesAsync();

        var reservation = Reservation.Create(studio.Id, Guid.NewGuid(),
            new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8)),
            "Dupont", [new PersonLine(ClientType.Owner, 2, 0)], studio.Capacity);
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        Assert.Contains(logs, l => l.Contains(reservation.Id.ToString()) && l.Contains("created"));
        // Events are cleared after dispatch so they fire exactly once.
        Assert.Empty(reservation.DomainEvents);
    }

    private sealed class CapturingLoggerProvider(List<string> sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(sink);
        public void Dispose() { }

        private sealed class CapturingLogger(List<string> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
                => sink.Add(formatter(state, exception));
        }
    }
}
