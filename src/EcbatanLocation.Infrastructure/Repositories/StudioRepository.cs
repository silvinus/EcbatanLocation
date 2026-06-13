using Microsoft.EntityFrameworkCore;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Infrastructure.Persistence;

namespace EcbatanLocation.Infrastructure.Repositories;

public class StudioRepository(EcbatanLocationDbContext context) : IStudioRepository
{
    public async Task<Studio?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Studios.FindAsync([id], ct);

    public async Task<IReadOnlyList<Studio>> GetAllAsync(CancellationToken ct = default)
        => await context.Studios.OrderBy(s => s.DisplayOrder).ToListAsync(ct);

    public async Task UpdateAsync(Studio studio, CancellationToken ct = default)
    {
        context.Studios.Update(studio);
        await context.SaveChangesAsync(ct);
    }

    public async Task AddAsync(Studio studio, CancellationToken ct = default)
    {
        context.Studios.Add(studio);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var studio = await context.Studios.FindAsync([id], ct)
                     ?? throw new InvalidOperationException($"Studio '{id}' not found.");
        context.Studios.Remove(studio);
        await context.SaveChangesAsync(ct);
    }

    public async Task<int> GetMaxDisplayOrderAsync(CancellationToken ct = default)
        => await context.Studios.AnyAsync(ct)
            ? await context.Studios.MaxAsync(s => s.DisplayOrder, ct)
            : 0;
}
