using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Exceptions;
using EcbatanLocation.Domain.ValueObjects;
using EcbatanLocation.Infrastructure.Persistence;
using EcbatanLocation.Infrastructure.Repositories;

namespace EcbatanLocation.Infrastructure.Tests.Persistence;

public class PerBedReservationRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    // A fresh DateRange instance per reservation: an owned type instance must not be shared
    // across entities or the EF change tracker rejects it.
    private static DateRange July() => new(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));

    private static Studio SeedPerBedStudio()
        => Studio.Create("Studio Centre", 4, false, false, 4, false, RentalMode.PerBed, 4);

    private static Reservation BedReservation(Studio studio, int beds, int adults)
        => Reservation.Create(
            studio.Id, Guid.NewGuid(), July(), "Tenant",
            [new PersonLine(ClientType.Owner, adults, 0)],
            studio.Capacity, studio.RentalMode, studio.NumberOfBeds, beds);

    [Fact]
    public async Task PerBed_ConcurrentReservationsWithinBeds_AreAllowed()
    {
        var studio = SeedPerBedStudio();

        await using var ctx = _factory.CreateContext();
        ctx.Studios.Add(studio);
        await ctx.SaveChangesAsync();

        var repo = new ReservationRepository(ctx);

        await repo.AddAsync(BedReservation(studio, beds: 2, adults: 2));
        await repo.AddAsync(BedReservation(studio, beds: 2, adults: 2));

        Assert.Equal(2, ctx.Reservations.Count());
    }

    [Fact]
    public async Task PerBed_ReservationExceedingBeds_Throws()
    {
        var studio = SeedPerBedStudio();

        await using var ctx = _factory.CreateContext();
        ctx.Studios.Add(studio);
        await ctx.SaveChangesAsync();

        var repo = new ReservationRepository(ctx);
        await repo.AddAsync(BedReservation(studio, beds: 3, adults: 1));

        await Assert.ThrowsAsync<NoBedsAvailableException>(
            () => repo.AddAsync(BedReservation(studio, beds: 2, adults: 1)));
    }

    [Fact]
    public async Task PerBed_ReservationExceedingCapacity_Throws()
    {
        var studio = SeedPerBedStudio(); // capacity 4

        await using var ctx = _factory.CreateContext();
        ctx.Studios.Add(studio);
        await ctx.SaveChangesAsync();

        var repo = new ReservationRepository(ctx);
        await repo.AddAsync(BedReservation(studio, beds: 1, adults: 3));

        // Beds fit (1 + 1) but people don't (3 + 2 > 4).
        await Assert.ThrowsAsync<NoBedsAvailableException>(
            () => repo.AddAsync(BedReservation(studio, beds: 1, adults: 2)));
    }

    [Fact]
    public async Task PerLodging_OverlappingReservation_StillThrows()
    {
        var studio = Studio.Create("Villa", 6, true, true, 1);

        await using var ctx = _factory.CreateContext();
        ctx.Studios.Add(studio);
        await ctx.SaveChangesAsync();

        var repo = new ReservationRepository(ctx);
        await repo.AddAsync(Reservation.Create(
            studio.Id, Guid.NewGuid(), July(), "A",
            [new PersonLine(ClientType.Owner, 2, 0)], studio.Capacity));

        await Assert.ThrowsAsync<OverlappingReservationException>(
            () => repo.AddAsync(Reservation.Create(
                studio.Id, Guid.NewGuid(), July(), "B",
                [new PersonLine(ClientType.Owner, 2, 0)], studio.Capacity)));
    }

    [Fact]
    public async Task BackfillBedCount_SetsAdultsClampedToBeds_OnlyForZeroBedCount()
    {
        var studio = SeedPerBedStudio(); // capacity 4, beds 4

        await using var ctx = _factory.CreateContext();
        ctx.Studios.Add(studio);
        await ctx.SaveChangesAsync();

        // Legacy reservation with BedCount 0 (created while the studio was whole-lodging).
        var legacy = Reservation.Create(
            studio.Id, Guid.NewGuid(), July(), "Legacy",
            [new PersonLine(ClientType.Owner, 2, 0)], studio.Capacity);
        // A reservation that already has a bed count must be left untouched.
        var existing = BedReservation(studio, beds: 1, adults: 1);

        ctx.Reservations.AddRange(legacy, existing);
        await ctx.SaveChangesAsync();

        var repo = new ReservationRepository(ctx);
        var updated = await repo.BackfillBedCountForStudioAsync(studio.Id, studio.NumberOfBeds);

        Assert.Equal(1, updated);
        var reloadedLegacy = await ctx.Reservations.FindAsync(legacy.Id);
        var reloadedExisting = await ctx.Reservations.FindAsync(existing.Id);
        Assert.Equal(2, reloadedLegacy!.BedCount); // adult count
        Assert.Equal(1, reloadedExisting!.BedCount); // unchanged
    }

    public void Dispose() => _factory.Dispose();
}
