using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Enums;
using PlanningLocation.Domain.ValueObjects;
using PlanningLocation.Infrastructure.Repositories;

namespace PlanningLocation.Infrastructure.Tests.Repositories;

public class ReservationRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly Guid _studioId;
    private readonly Guid _ownerId;

    public ReservationRepositoryTests()
    {
        using var ctx = _factory.CreateContext();
        var studio = Studio.Create("Villa", 6, true, true, 1);
        var owner = Owner.Create("Léa", "user-1");
        ctx.Studios.Add(studio);
        ctx.Owners.Add(owner);
        ctx.SaveChanges();
        _studioId = studio.Id;
        _ownerId = owner.Id;
    }

    private static Reservation MakeReservation(Guid studioId, Guid ownerId, DateRange dates,
        string tenant = "Dupont", int adults = 2, int children = 0, ClientType type = ClientType.Owner, int capacity = 6)
    {
        var lines = new[] { new PersonLine(type, adults, children) };
        return Reservation.Create(studioId, ownerId, dates, tenant, lines, capacity);
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_RoundTrips()
    {
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var reservation = MakeReservation(_studioId, _ownerId, dates, adults: 2, children: 1);

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            await repo.AddAsync(reservation);
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            var loaded = await repo.GetByIdAsync(reservation.Id);
            Assert.NotNull(loaded);
            Assert.Equal("Dupont", loaded.TenantName);
            Assert.Equal(new DateOnly(2026, 7, 1), loaded.Dates.StartDate);
            Assert.Equal(new DateOnly(2026, 7, 8), loaded.Dates.EndDate);
            Assert.Equal(ReservationStatus.Pending, loaded.Status);
            Assert.Single(loaded.PersonLines);
        }
    }

    [Fact]
    public async Task ExistsOverlapAsync_DetectsOverlap()
    {
        var dates = new DateRange(new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 12));
        var reservation = MakeReservation(_studioId, _ownerId, dates, "Martin", type: ClientType.Acquaintance);

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            await repo.AddAsync(reservation);
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);

            var overlapping = new DateRange(new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 15));
            Assert.True(await repo.ExistsOverlapAsync(_studioId, overlapping));

            var noOverlap = new DateRange(new DateOnly(2026, 7, 12), new DateOnly(2026, 7, 20));
            Assert.False(await repo.ExistsOverlapAsync(_studioId, noOverlap));
        }
    }

    [Fact]
    public async Task ExistsOverlapAsync_ExcludesSpecifiedReservation()
    {
        var dates = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 8));
        var reservation = MakeReservation(_studioId, _ownerId, dates, "Durand", adults: 1);

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            await repo.AddAsync(reservation);
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            Assert.True(await repo.ExistsOverlapAsync(_studioId, dates));
            Assert.False(await repo.ExistsOverlapAsync(_studioId, dates, reservation.Id));
        }
    }

    [Fact]
    public async Task GetByMonthAsync_ReturnsReservationsOverlappingMonth()
    {
        var july = new DateRange(new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 20));
        var august = new DateRange(new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 15));
        var crossMonth = new DateRange(new DateOnly(2026, 7, 25), new DateOnly(2026, 8, 5));

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            await repo.AddAsync(MakeReservation(_studioId, _ownerId, july, "A", adults: 1));
            await repo.AddAsync(MakeReservation(_studioId, _ownerId, august, "B", adults: 1));
            await repo.AddAsync(MakeReservation(_studioId, _ownerId, crossMonth, "C", adults: 1));
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            var julyResults = await repo.GetByMonthAsync(2026, 7);
            Assert.Equal(2, julyResults.Count);

            var augustResults = await repo.GetByMonthAsync(2026, 8);
            Assert.Equal(2, augustResults.Count);
        }
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var dates = new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5));
        var reservation = MakeReservation(_studioId, _ownerId, dates, "Original");

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            await repo.AddAsync(reservation);
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            var loaded = await repo.GetByIdAsync(reservation.Id);
            Assert.NotNull(loaded);

            var newDates = new DateRange(new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 8));
            var newLines = new[] { new PersonLine(ClientType.Acquaintance, 3, 1) };
            loaded.Update(newDates, "Updated", newLines, 6);
            await repo.UpdateAsync(loaded);
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            var reloaded = await repo.GetByIdAsync(reservation.Id);
            Assert.NotNull(reloaded);
            Assert.Equal("Updated", reloaded.TenantName);
            Assert.Equal(3, reloaded.TotalAdultCount);
            Assert.Equal(new DateOnly(2026, 9, 2), reloaded.Dates.StartDate);
        }
    }

    [Fact]
    public async Task DeleteAsync_RemovesReservation()
    {
        var dates = new DateRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5));
        var reservation = MakeReservation(_studioId, _ownerId, dates, "ToDelete", adults: 1);

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            await repo.AddAsync(reservation);
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            await repo.DeleteAsync(reservation.Id);
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            Assert.Null(await repo.GetByIdAsync(reservation.Id));
        }
    }

    [Fact]
    public async Task GetByDateAsync_ReturnsActiveReservationsForDay()
    {
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10));
        var reservation = MakeReservation(_studioId, _ownerId, dates, "Test");

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            await repo.AddAsync(reservation);
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);

            var inRange = await repo.GetByDateAsync(new DateOnly(2026, 7, 5));
            Assert.Single(inRange);

            var departureDay = await repo.GetByDateAsync(new DateOnly(2026, 7, 10));
            Assert.Empty(departureDay);

            var arrivalDay = await repo.GetByDateAsync(new DateOnly(2026, 7, 1));
            Assert.Single(arrivalDay);
        }
    }

    [Fact]
    public async Task GetByOwnerAndOverlappingDatesAsync_FiltersCorrectly()
    {
        var owner2 = Owner.Create("Sarah", "user-2");
        await using (var ctx = _factory.CreateContext())
        {
            ctx.Owners.Add(owner2);
            await ctx.SaveChangesAsync();
        }

        var dates1 = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10));
        var dates2 = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10));
        var r1 = MakeReservation(_studioId, _ownerId, dates1, "ByOwner1", adults: 1);
        var r2 = MakeReservation(_studioId, owner2.Id, dates2, "ByOwner2", adults: 1);

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            await repo.AddAsync(r1);
            await repo.AddAsync(r2);
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new ReservationRepository(ctx);
            var search = new DateRange(new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 15));
            var results = await repo.GetByOwnerAndOverlappingDatesAsync(_ownerId, search);
            Assert.Single(results);
            Assert.Equal("ByOwner1", results[0].TenantName);
        }
    }

    public void Dispose() => _factory.Dispose();
}
