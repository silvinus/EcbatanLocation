using PlanningLocation.Domain.Entities;
using PlanningLocation.Infrastructure.Repositories;

namespace PlanningLocation.Infrastructure.Tests.Repositories;

public class StudioRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task GetAllAsync_ReturnsStudiosOrderedByDisplayOrder()
    {
        await using (var ctx = _factory.CreateContext())
        {
            ctx.Studios.AddRange(
                Studio.Create("Studio B", 2, true, true, 3),
                Studio.Create("Studio A", 4, false, true, 1),
                Studio.Create("Studio C", 6, true, false, 2));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new StudioRepository(ctx);
            var studios = await repo.GetAllAsync();
            Assert.Equal(3, studios.Count);
            Assert.Equal("Studio A", studios[0].Name);
            Assert.Equal("Studio C", studios[1].Name);
            Assert.Equal("Studio B", studios[2].Name);
        }
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectStudio()
    {
        var studio = Studio.Create("Villa", 6, true, true, 1);
        await using (var ctx = _factory.CreateContext())
        {
            ctx.Studios.Add(studio);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new StudioRepository(ctx);
            var loaded = await repo.GetByIdAsync(studio.Id);
            Assert.NotNull(loaded);
            Assert.Equal("Villa", loaded.Name);
            Assert.Equal(6, loaded.Capacity);
            Assert.True(loaded.HasKitchen);
            Assert.True(loaded.RentableAlone);
        }
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var ctx = _factory.CreateContext();
        var repo = new StudioRepository(ctx);
        Assert.Null(await repo.GetByIdAsync(Guid.NewGuid()));
    }

    public void Dispose() => _factory.Dispose();
}
