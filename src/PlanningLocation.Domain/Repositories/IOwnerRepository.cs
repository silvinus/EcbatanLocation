using PlanningLocation.Domain.Entities;

namespace PlanningLocation.Domain.Repositories;

public interface IOwnerRepository
{
    Task<Owner?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Owner?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken ct = default);
}
