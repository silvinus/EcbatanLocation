using Microsoft.EntityFrameworkCore;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Repositories;
using PlanningLocation.Infrastructure.Persistence;

namespace PlanningLocation.Infrastructure.Repositories;

public class ProprietaireRepository(PlanningLocationDbContext context) : IProprietaireRepository
{
    public async Task<Proprietaire?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Proprietaires.FindAsync([id], ct);

    public async Task<Proprietaire?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => await context.Proprietaires.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task<IReadOnlyList<Proprietaire>> GetAllAsync(CancellationToken ct = default)
        => await context.Proprietaires.OrderBy(p => p.Nom).ToListAsync(ct);
}
