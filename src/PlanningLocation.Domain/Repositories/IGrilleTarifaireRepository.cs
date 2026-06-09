using PlanningLocation.Domain.Entities;

namespace PlanningLocation.Domain.Repositories;

public interface IGrilleTarifaireRepository
{
    Task<GrilleTarifaire?> GetByAnneeAsync(int annee, CancellationToken ct = default);
    Task AddAsync(GrilleTarifaire grille, CancellationToken ct = default);
    Task UpdateAsync(GrilleTarifaire grille, CancellationToken ct = default);
}
