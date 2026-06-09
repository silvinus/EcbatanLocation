using Microsoft.EntityFrameworkCore;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Enums;
using PlanningLocation.Domain.ValueObjects;

namespace PlanningLocation.Infrastructure.Tests.Persistence;

public class EfCoreMappingTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task DateRange_OwnedType_MapsToColumns()
    {
        var studio = Studio.Create("Villa", 6, true, true, 1);
        var owner = Owner.Create("Léa", "u1");
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var reservation = Reservation.Create(studio.Id, owner.Id, dates, "Test", 2, 0, ClientType.Owner, 6);

        await using (var ctx = _factory.CreateContext())
        {
            ctx.Studios.Add(studio);
            ctx.Owners.Add(owner);
            ctx.Reservations.Add(reservation);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _factory.CreateContext())
        {
            var loaded = await ctx.Reservations.FirstAsync();
            Assert.Equal(new DateOnly(2026, 7, 1), loaded.Dates.StartDate);
            Assert.Equal(new DateOnly(2026, 7, 8), loaded.Dates.EndDate);
        }
    }

    [Fact]
    public async Task EnumConversions_StoredAsStrings()
    {
        var studio = Studio.Create("Villa", 6, true, true, 1);
        var owner = Owner.Create("Léa", "u1");
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var reservation = Reservation.Create(studio.Id, owner.Id, dates, "Test", 2, 0, ClientType.Acquaintance, 6);

        await using (var ctx = _factory.CreateContext())
        {
            ctx.Studios.Add(studio);
            ctx.Owners.Add(owner);
            ctx.Reservations.Add(reservation);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _factory.CreateContext())
        {
            var raw = await ctx.Database.SqlQueryRaw<string>(
                "SELECT ClientType AS Value FROM Reservations LIMIT 1").FirstAsync();
            Assert.Equal("Acquaintance", raw);

            var rawStatus = await ctx.Database.SqlQueryRaw<string>(
                "SELECT Status AS Value FROM Reservations LIMIT 1").FirstAsync();
            Assert.Equal("Pending", rawStatus);
        }
    }

    [Fact]
    public async Task PricingGrid_YearUniqueness_EnforcedByDb()
    {
        await using var ctx = _factory.CreateContext();
        var grid1 = PricingGrid.Create(2026, [PricingLine.Create(ClientType.Owner, 3.50m)]);
        var grid2 = PricingGrid.Create(2026, [PricingLine.Create(ClientType.Tent, 7.00m)]);

        ctx.PricingGrids.Add(grid1);
        await ctx.SaveChangesAsync();

        ctx.PricingGrids.Add(grid2);
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Owner_UserIdUniqueness_EnforcedByDb()
    {
        await using var ctx = _factory.CreateContext();
        ctx.Owners.Add(Owner.Create("Léa", "same-user-id"));
        await ctx.SaveChangesAsync();

        ctx.Owners.Add(Owner.Create("Clone", "same-user-id"));
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task PricingGrid_CascadeDelete_RemovesLines()
    {
        var grid = PricingGrid.Create(2026, [
            PricingLine.Create(ClientType.Owner, 3.50m),
            PricingLine.Create(ClientType.Tent, 7.00m)
        ]);

        await using (var ctx = _factory.CreateContext())
        {
            ctx.PricingGrids.Add(grid);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _factory.CreateContext())
        {
            var loaded = await ctx.PricingGrids.Include(g => g.Lines).FirstAsync();
            ctx.PricingGrids.Remove(loaded);
            await ctx.SaveChangesAsync();

            Assert.Empty(await ctx.PricingLines.ToListAsync());
        }
    }

    public void Dispose() => _factory.Dispose();
}
