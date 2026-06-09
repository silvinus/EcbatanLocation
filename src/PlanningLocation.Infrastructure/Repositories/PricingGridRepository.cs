using Microsoft.EntityFrameworkCore;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Repositories;
using PlanningLocation.Infrastructure.Persistence;

namespace PlanningLocation.Infrastructure.Repositories;

public class PricingGridRepository(PlanningLocationDbContext context) : IPricingGridRepository
{
    public async Task<PricingGrid?> GetByYearAsync(int year, CancellationToken ct = default)
        => await context.PricingGrids
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Year == year, ct);

    public async Task AddAsync(PricingGrid grid, CancellationToken ct = default)
    {
        await context.PricingGrids.AddAsync(grid, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PricingGrid grid, CancellationToken ct = default)
    {
        context.PricingGrids.Update(grid);
        await context.SaveChangesAsync(ct);
    }
}
