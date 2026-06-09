using PlanningLocation.Domain.Entities;
using PlanningLocation.Infrastructure.Repositories;

namespace PlanningLocation.Infrastructure.Tests.Repositories;

public class OwnerRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task GetByUserIdAsync_ReturnsCorrectOwner()
    {
        var owner = Owner.Create("Jean", "identity-user-42");
        await using (var ctx = _factory.CreateContext())
        {
            ctx.Owners.Add(owner);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new OwnerRepository(ctx);
            var loaded = await repo.GetByUserIdAsync("identity-user-42");
            Assert.NotNull(loaded);
            Assert.Equal("Jean", loaded.Name);
        }
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var ctx = _factory.CreateContext();
        var repo = new OwnerRepository(ctx);
        Assert.Null(await repo.GetByUserIdAsync("nonexistent"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOwnersOrderedByName()
    {
        await using (var ctx = _factory.CreateContext())
        {
            ctx.Owners.AddRange(
                Owner.Create("Sarah", "u1"),
                Owner.Create("Christophe", "u2"),
                Owner.Create("Léa", "u3"));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _factory.CreateContext())
        {
            var repo = new OwnerRepository(ctx);
            var owners = await repo.GetAllAsync();
            Assert.Equal(3, owners.Count);
            Assert.Equal("Christophe", owners[0].Name);
            Assert.Equal("Léa", owners[1].Name);
            Assert.Equal("Sarah", owners[2].Name);
        }
    }

    public void Dispose() => _factory.Dispose();
}
