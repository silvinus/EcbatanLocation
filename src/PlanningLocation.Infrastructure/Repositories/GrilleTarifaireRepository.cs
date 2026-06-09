using Microsoft.EntityFrameworkCore;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Repositories;
using PlanningLocation.Infrastructure.Persistence;

namespace PlanningLocation.Infrastructure.Repositories;

public class GrilleTarifaireRepository(PlanningLocationDbContext context) : IGrilleTarifaireRepository
{
    public async Task<GrilleTarifaire?> GetByAnneeAsync(int annee, CancellationToken ct = default)
        => await context.GrillesTarifaires
            .Include(g => g.Lignes)
            .FirstOrDefaultAsync(g => g.Annee == annee, ct);

    public async Task AddAsync(GrilleTarifaire grille, CancellationToken ct = default)
    {
        await context.GrillesTarifaires.AddAsync(grille, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(GrilleTarifaire grille, CancellationToken ct = default)
    {
        context.GrillesTarifaires.Update(grille);
        await context.SaveChangesAsync(ct);
    }
}
