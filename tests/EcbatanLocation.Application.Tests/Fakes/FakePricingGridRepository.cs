using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Tests.Fakes;

/// <summary>In-memory <see cref="IPricingGridRepository"/> for handler tests.</summary>
public sealed class FakePricingGridRepository : IPricingGridRepository
{
    public List<PricingGrid> Items { get; } = [];
    public int AddCount { get; private set; }
    public int UpdateCount { get; private set; }

    public Task<PricingGrid?> GetByYearAsync(int year, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(g => g.Year == year));

    public Task AddAsync(PricingGrid grid, CancellationToken ct = default)
    {
        Items.Add(grid);
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PricingGrid grid, CancellationToken ct = default)
    {
        UpdateCount++;
        return Task.CompletedTask;
    }
}
