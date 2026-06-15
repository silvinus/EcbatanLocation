using EcbatanLocation.Application;
using EcbatanLocation.Application.Events;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Events;
using EcbatanLocation.Domain.ValueObjects;
using EcbatanLocation.Infrastructure.Persistence;

namespace EcbatanLocation.Infrastructure.Tests.Persistence;

public class DomainEventCollectorInterceptorTests
{
    [Fact]
    public async Task SavingChanges_CollectsDomainEvents_IntoAccumulatorAndClearsAggregate()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var accumulator = scope.ServiceProvider.GetRequiredService<IDomainEventAccumulator>();

        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<EcbatanLocationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new DomainEventCollectorInterceptor(accumulator))
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

        // The interceptor only buffers; it does not dispatch. The event sits in the accumulator
        // and is cleared from the aggregate so it can never fire twice.
        Assert.Empty(reservation.DomainEvents);

        var collected = accumulator.Collect();
        Assert.Contains(collected, e => e is ReservationCreated c && c.ReservationId == reservation.Id);

        // Collect() drains the buffer.
        Assert.Empty(accumulator.Collect());
    }
}
