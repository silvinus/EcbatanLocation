using PlanningLocation.Domain.Entities;

namespace PlanningLocation.Domain.Repositories;

public interface IProprietaireRepository
{
    Task<Proprietaire?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Proprietaire?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Proprietaire>> GetAllAsync(CancellationToken ct = default);
}
