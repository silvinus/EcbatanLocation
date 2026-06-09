using Microsoft.EntityFrameworkCore;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Repositories;
using PlanningLocation.Infrastructure.Persistence;

namespace PlanningLocation.Infrastructure.Repositories;

public class OwnerRepository(PlanningLocationDbContext context) : IOwnerRepository
{
    public async Task<Owner?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Owners.FindAsync([id], ct);

    public async Task<Owner?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => await context.Owners.FirstOrDefaultAsync(o => o.UserId == userId, ct);

    public async Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken ct = default)
        => await context.Owners.OrderBy(o => o.Name).ToListAsync(ct);
}
