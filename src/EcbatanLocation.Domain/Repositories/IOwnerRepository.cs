using EcbatanLocation.Domain.Entities;

namespace EcbatanLocation.Domain.Repositories;

public interface IOwnerRepository
{
    Task<Owner?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Owner?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Owner owner, CancellationToken ct = default);
    Task UpdateAsync(Owner owner, CancellationToken ct = default);
    Task DeleteAsync(Owner owner, CancellationToken ct = default);
}
