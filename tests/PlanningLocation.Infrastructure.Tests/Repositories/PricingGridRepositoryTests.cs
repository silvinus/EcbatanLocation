using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Enums;
using PlanningLocation.Infrastructure.Repositories;

namespace PlanningLocation.Infrastructure.Tests.Repositories;

public class PricingGridRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task AddAsync_And_GetByYearAsync_RoundTrips()
    {
        var lines = new[]
        {
            PricingLine.Create(ClientType.Owner, 3.50m),
            PricingLine.Create(ClientType.Acquaintance, 15.00m),
        };
        var grid = PricingGrid.Create(2026, lines);

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new PricingGridRepository(ctx);
            await repo.AddAsync(grid);
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new PricingGridRepository(ctx);
            var loaded = await repo.GetByYearAsync(2026);
            Assert.NotNull(loaded);
            Assert.Equal(2026, loaded.Year);
            Assert.Equal(2, loaded.Lines.Count);
            Assert.Equal(3.50m, loaded.GetRate(ClientType.Owner));
            Assert.Equal(15.00m, loaded.GetRate(ClientType.Acquaintance));
        }
    }

    [Fact]
    public async Task GetByYearAsync_ReturnsNull_WhenYearNotFound()
    {
        await using var ctx = _factory.CreateContext();
        var repo = new PricingGridRepository(ctx);
        Assert.Null(await repo.GetByYearAsync(2099));
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var lines = new[] { PricingLine.Create(ClientType.Owner, 3.50m) };
        var grid = PricingGrid.Create(2027, lines);

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new PricingGridRepository(ctx);
            await repo.AddAsync(grid);
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new PricingGridRepository(ctx);
            var loaded = await repo.GetByYearAsync(2027);
            Assert.NotNull(loaded);

            loaded.Update([PricingLine.Create(ClientType.Owner, 5.00m), PricingLine.Create(ClientType.Tent, 8.00m)]);
            await repo.UpdateAsync(loaded);
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new PricingGridRepository(ctx);
            var reloaded = await repo.GetByYearAsync(2027);
            Assert.NotNull(reloaded);
            Assert.Equal(2, reloaded.Lines.Count);
            Assert.Equal(5.00m, reloaded.GetRate(ClientType.Owner));
        }
    }

    public void Dispose() => _factory.Dispose();
}
