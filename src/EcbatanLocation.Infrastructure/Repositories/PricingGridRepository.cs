using Microsoft.EntityFrameworkCore;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Infrastructure.Persistence;

namespace EcbatanLocation.Infrastructure.Repositories;

public class PricingGridRepository(EcbatanLocationDbContext context) : IPricingGridRepository
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
        var oldLines = await context.PricingLines
            .Where(l => l.PricingGridId == grid.Id)
            .ToListAsync(ct);
        context.PricingLines.RemoveRange(oldLines);
        context.PricingLines.AddRange(grid.Lines);

        await context.SaveChangesAsync(ct);
    }
}
