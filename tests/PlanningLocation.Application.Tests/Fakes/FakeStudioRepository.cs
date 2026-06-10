using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Repositories;

namespace PlanningLocation.Application.Tests.Fakes;

/// <summary>In-memory <see cref="IStudioRepository"/> for handler tests.</summary>
public sealed class FakeStudioRepository(params Studio[] studios) : IStudioRepository
{
    public List<Studio> Items { get; } = [.. studios];

    public Task<Studio?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(s => s.Id == id));

    public Task<IReadOnlyList<Studio>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Studio>)Items);
}
