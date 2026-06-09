using Microsoft.EntityFrameworkCore;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Repositories;
using PlanningLocation.Infrastructure.Persistence;

namespace PlanningLocation.Infrastructure.Repositories;

public class StudioRepository(PlanningLocationDbContext context) : IStudioRepository
{
    public async Task<Studio?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Studios.FindAsync([id], ct);

    public async Task<IReadOnlyList<Studio>> GetAllAsync(CancellationToken ct = default)
        => await context.Studios.OrderBy(s => s.DisplayOrder).ToListAsync(ct);
}
